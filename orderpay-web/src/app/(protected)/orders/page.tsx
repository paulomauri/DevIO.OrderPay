"use client";

import { useQuery } from "@tanstack/react-query";
import styled from "styled-components";
import { useAppDispatch } from "@/store";
import { openModal } from "@/store/uiSlice";
import { Card, CardHeader, CardTitle } from "@/components/ui/Card";
import { TableWrapper, Table, Thead, Tbody, Tr, Th, Td } from "@/components/ui/Table";
import { OrderStatusBadge } from "@/components/ui/Badge";
import Button from "@/components/ui/Button";
import AdminOnly from "@/components/ui/AdminOnly";
import Spinner from "@/components/ui/Spinner";
import { ordersService } from "@/services/orders";

const Center = styled.div`
  display:         flex;
  justify-content: center;
  padding:         ${({ theme }) => theme.spacing.xxl};
`;

const Empty = styled.p`
  text-align: center;
  color:      ${({ theme }) => theme.colors.textMuted};
  padding:    ${({ theme }) => theme.spacing.xxl};
`;

const ActionsCell = styled(Td)`
  display:     flex;
  gap:         ${({ theme }) => theme.spacing.xs};
  align-items: center;
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
  const { data: orders, isLoading } = useQuery({
    queryKey: ["orders"],
    queryFn:  ordersService.getAll,
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>Orders</CardTitle>
        <Button size="sm" onClick={() => dispatch(openModal({ modal: "createOrder" }))}>
          + New Order
        </Button>
      </CardHeader>

      {isLoading && (
        <Center><Spinner /></Center>
      )}

      {!isLoading && (!orders || orders.length === 0) && (
        <Empty>No orders found.</Empty>
      )}

      {!isLoading && orders && orders.length > 0 && (
        <TableWrapper>
          <Table>
            <Thead>
              <Tr>
                <Th>Order ID</Th>
                <Th>Date</Th>
                <Th>Status</Th>
                <Th>Total</Th>
                <Th>Actions</Th>
              </Tr>
            </Thead>
            <Tbody>
              {orders.map((o) => (
                <Tr key={o.id}>
                  <Td><ShortId>{o.id.slice(0, 8)}…</ShortId></Td>
                  <Td>{formatDate(o.orderDate)}</Td>
                  <Td><OrderStatusBadge status={o.status} /></Td>
                  <Td>${(o.totalPrice - o.totalDiscount).toFixed(2)}</Td>
                  <ActionsCell>
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
                        onClick={() => dispatch(openModal({ modal: "confirmDelete", payload: o.id }))}
                      >
                        Delete
                      </Button>
                    </AdminOnly>
                  </ActionsCell>
                </Tr>
              ))}
            </Tbody>
          </Table>
        </TableWrapper>
      )}
    </Card>
  );
}
