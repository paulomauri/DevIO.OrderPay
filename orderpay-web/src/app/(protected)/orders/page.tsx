"use client";

import { useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import styled from "styled-components";
import { useAppDispatch, useAppSelector } from "@/store";
import { openModal, selectSettlingOrders, clearOrderSettling } from "@/store/uiSlice";
import { Card, CardHeader, CardTitle } from "@/components/ui/Card";
import { TableWrapper, Table, Thead, Tbody, Tr, Th, Td } from "@/components/ui/Table";
import { OrderStatusBadge, PaymentProcessingBadge } from "@/components/ui/Badge";
import Button from "@/components/ui/Button";
import AdminOnly from "@/components/ui/AdminOnly";
import TableSkeleton from "@/components/ui/TableSkeleton";
import EmptyState from "@/components/ui/EmptyState";
import { ordersService } from "@/services/orders";
import { isOrderEditable, type OrderResponse } from "@/types/order";

// Stop showing the optimistic badge (and polling) after this long even if the order never
// advanced — guards against a stalled broker leaving a row "Payment Processing…" forever.
const SETTLE_TIMEOUT_MS = 30_000;

// Fixed 4-column grid so every row's actions (Pay · Edit · Status · Delete) line
// up in the same columns; buttons fill their cell for an even, aligned look.
const ActionsCell = styled(Td)`
  display:               grid;
  grid-template-columns: repeat(4, minmax(56px, 1fr));
  gap:                   ${({ theme }) => theme.spacing.xs};
  align-items:           center;

  & > * { width: 100%; }
`;

const ShortId = styled.span`
  font-family: monospace;
  font-size:   ${({ theme }) => theme.typography.fontSize.xs};
  color:       ${({ theme }) => theme.colors.textMuted};
`;

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString("en-US", {
    year: "numeric", month: "short", day: "numeric",
  });
}

export default function OrdersPage() {
  const dispatch = useAppDispatch();
  const settling = useAppSelector(selectSettlingOrders);

  // A just-paid order is "settling" until its server status leaves the editable window
  // (isOrderEditable flips false exactly at PaymentConfirmed — the async advance landed).
  const isSettling = (o: OrderResponse) => o.id in settling && isOrderEditable(o.status);

  const { data: orders, isLoading, isError } = useQuery({
    queryKey: ["orders"],
    queryFn:  ordersService.getAll,
    // Poll only while something is settling, then fall idle.
    refetchInterval: (query) => {
      const data = query.state.data as OrderResponse[] | undefined;
      return data?.some(isSettling) ? 1200 : false;
    },
  });

  // Drop orders that have confirmed, timed out, or vanished from the settling set.
  useEffect(() => {
    if (!orders) return;
    const now = Date.now();
    for (const id of Object.keys(settling)) {
      const order = orders.find((o) => o.id === id);
      if (!order || !isOrderEditable(order.status) || now - settling[id] > SETTLE_TIMEOUT_MS) {
        dispatch(clearOrderSettling(id));
      }
    }
  }, [orders, settling, dispatch]);

  const isEmpty = !isLoading && !isError && (!orders || orders.length === 0);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Orders</CardTitle>
        <Button size="sm" onClick={() => dispatch(openModal({ modal: "createOrder" }))}>
          + New Order
        </Button>
      </CardHeader>

      {isError  && <EmptyState message="Failed to load orders." icon="⚠️" />}
      {isEmpty  && <EmptyState message="No orders yet." icon="🛒" />}

      {!isEmpty && (
        <TableWrapper>
          <Table>
            <Thead>
              <Tr>
                <Th>Order ID</Th>
                <Th>Date</Th>
                <Th>Status</Th>
                <Th>Discount</Th>
                <Th>Total</Th>
                <Th>Actions</Th>
              </Tr>
            </Thead>
            <Tbody>
              {isLoading ? (
                <TableSkeleton cols={6} />
              ) : (
                orders!.map((o) => (
                  <Tr key={o.id}>
                    <Td><ShortId>{o.id.slice(0, 8)}…</ShortId></Td>
                    <Td>{formatDate(o.orderDate)}</Td>
                    <Td>
                      {isSettling(o)
                        ? <PaymentProcessingBadge />
                        : <OrderStatusBadge status={o.status} />}
                    </Td>
                    <Td>${o.totalDiscount.toFixed(2)}</Td>
                    <Td>${(o.totalPrice - o.totalDiscount).toFixed(2)}</Td>
                    <ActionsCell>
                      <Button
                        size="sm"
                        disabled={!isOrderEditable(o.status) || isSettling(o)}
                        onClick={() => dispatch(openModal({ modal: "payOrder", payload: o.id }))}
                      >
                        Pay
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        disabled={!isOrderEditable(o.status) || isSettling(o)}
                        onClick={() => dispatch(openModal({ modal: "editOrder", payload: o.id }))}
                      >
                        Edit
                      </Button>
                      <AdminOnly>
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => dispatch(openModal({ modal: "updateStatus", payload: o.id }))}
                        >
                          Status
                        </Button>
                      </AdminOnly>
                      <AdminOnly>
                        <Button
                          size="sm"
                          variant="danger"
                          onClick={() => dispatch(openModal({ modal: "confirmDelete", payload: "order:" + o.id }))}
                        >
                          Delete
                        </Button>
                      </AdminOnly>
                    </ActionsCell>
                  </Tr>
                ))
              )}
            </Tbody>
          </Table>
        </TableWrapper>
      )}
    </Card>
  );
}
