"use client";

import { useQuery } from "@tanstack/react-query";
import styled from "styled-components";
import { Card, CardHeader, CardTitle } from "@/components/ui/Card";
import { customersService } from "@/services/customers";
import { productsService  } from "@/services/products";
import { ordersService    } from "@/services/orders";
import type { OrderStatus } from "@/types/order";

// ── Styled ────────────────────────────────────────────────────────────────────

const Grid = styled.div`
  display:               grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap:                   ${({ theme }) => theme.spacing.lg};
  margin-bottom:         ${({ theme }) => theme.spacing.xl};
`;

const StatNumber = styled.p`
  font-size:   ${({ theme }) => theme.typography.fontSize.xxl};
  font-weight: ${({ theme }) => theme.typography.fontWeight.bold};
  color:       ${({ theme }) => theme.colors.text};
  margin:      ${({ theme }) => theme.spacing.sm} 0 0;
`;

const StatLabel = styled.p`
  font-size: ${({ theme }) => theme.typography.fontSize.sm};
  color:     ${({ theme }) => theme.colors.textMuted};
  margin:    0;
`;

const StatusGrid = styled.div`
  display:               grid;
  grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
  gap:                   ${({ theme }) => theme.spacing.sm};
`;

const StatusCard = styled.div<{ $color: string }>`
  background:    ${({ $color }) => `${$color}12`};
  border:        1px solid ${({ $color }) => `${$color}30`};
  border-radius: ${({ theme }) => theme.borderRadius.md};
  padding:       ${({ theme }) => theme.spacing.md};
`;

const StatusCount = styled.p<{ $color: string }>`
  font-size:   ${({ theme }) => theme.typography.fontSize.xl};
  font-weight: ${({ theme }) => theme.typography.fontWeight.bold};
  color:       ${({ $color }) => $color};
  margin:      0 0 4px;
`;

const StatusName = styled.p`
  font-size: ${({ theme }) => theme.typography.fontSize.xs};
  color:     ${({ theme }) => theme.colors.textMuted};
  margin:    0;
`;

// ── Status breakdown config ───────────────────────────────────────────────────

const statusConfig: { status: OrderStatus; label: string; color: string }[] = [
  { status: "Pending",               label: "Pending",               color: "#F59E0B" },
  { status: "AwaitingPayment",       label: "Awaiting Payment",      color: "#3B82F6" },
  { status: "PaymentConfirmed",      label: "Payment Confirmed",     color: "#10B981" },
  { status: "Processing",            label: "Processing",            color: "#8B5CF6" },
  { status: "Shipped",               label: "Shipped",               color: "#06B6D4" },
  { status: "Delivered",             label: "Delivered",             color: "#16A34A" },
  { status: "CancellationRequested", label: "Cancellation Req.",     color: "#F97316" },
  { status: "Refunding",             label: "Refunding",             color: "#EF4444" },
  { status: "Cancelled",             label: "Cancelled",             color: "#6B7280" },
];

// ── Component ─────────────────────────────────────────────────────────────────

export default function DashboardPage() {
  const { data: customers } = useQuery({ queryKey: ["customers"], queryFn: customersService.getAll });
  const { data: products  } = useQuery({ queryKey: ["products"],  queryFn: productsService.getAll  });
  const { data: orders    } = useQuery({ queryKey: ["orders"],    queryFn: ordersService.getAll    });

  const totalRevenue = orders?.reduce((sum, o) => sum + o.totalPrice - o.totalDiscount, 0) ?? 0;

  return (
    <>
      <Grid>
        <Card>
          <StatLabel>Customers</StatLabel>
          <StatNumber>{customers?.length ?? "—"}</StatNumber>
        </Card>
        <Card>
          <StatLabel>Products</StatLabel>
          <StatNumber>{products?.length ?? "—"}</StatNumber>
        </Card>
        <Card>
          <StatLabel>Total Orders</StatLabel>
          <StatNumber>{orders?.length ?? "—"}</StatNumber>
        </Card>
        <Card>
          <StatLabel>Net Revenue</StatLabel>
          <StatNumber>
            {orders
              ? `$${totalRevenue.toFixed(2)}`
              : "—"}
          </StatNumber>
        </Card>
      </Grid>

      <Card>
        <CardHeader>
          <CardTitle>Orders by Status</CardTitle>
        </CardHeader>
        <StatusGrid>
          {statusConfig.map(({ status, label, color }) => {
            const count = orders?.filter((o) => o.status === status).length ?? 0;
            return (
              <StatusCard key={status} $color={color}>
                <StatusCount $color={color}>{count}</StatusCount>
                <StatusName>{label}</StatusName>
              </StatusCard>
            );
          })}
        </StatusGrid>
      </Card>
    </>
  );
}
