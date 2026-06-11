"use client";

import styled from "styled-components";
import type { OrderStatus } from "@/types/order";

interface BadgeProps {
  color:    string;
  children: React.ReactNode;
}

const StyledBadge = styled.span<{ $color: string }>`
  display:       inline-flex;
  align-items:   center;
  padding:       2px 10px;
  border-radius: ${({ theme }) => theme.borderRadius.full};
  font-size:     ${({ theme }) => theme.typography.fontSize.xs};
  font-weight:   ${({ theme }) => theme.typography.fontWeight.medium};
  background:    ${({ $color }) => `${$color}1A`}; /* 10% opacity fill */
  color:         ${({ $color }) => $color};
  white-space:   nowrap;
`;

export default function Badge({ color, children }: BadgeProps) {
  return <StyledBadge $color={color}>{children}</StyledBadge>;
}

// ── Order status convenience component ──────────────────────

const statusColors: Record<OrderStatus, string> = {
  Pending:               "#F59E0B",
  AwaitingPayment:       "#3B82F6",
  PaymentConfirmed:      "#10B981",
  Processing:            "#8B5CF6",
  Shipped:               "#06B6D4",
  Delivered:             "#16A34A",
  CancellationRequested: "#F97316",
  Refunding:             "#EF4444",
  Cancelled:             "#6B7280",
};

const statusLabels: Record<OrderStatus, string> = {
  Pending:               "Pending",
  AwaitingPayment:       "Awaiting Payment",
  PaymentConfirmed:      "Payment Confirmed",
  Processing:            "Processing",
  Shipped:               "Shipped",
  Delivered:             "Delivered",
  CancellationRequested: "Cancellation Requested",
  Refunding:             "Refunding",
  Cancelled:             "Cancelled",
};

export function OrderStatusBadge({ status }: { status: OrderStatus }) {
  return <Badge color={statusColors[status]}>{statusLabels[status]}</Badge>;
}
