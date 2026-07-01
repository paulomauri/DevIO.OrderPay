import { renderWithProviders, screen } from "@/test/test-utils";
import Badge, { OrderStatusBadge, PaymentProcessingBadge } from "./Badge";
import type { OrderStatus } from "@/types/order";

describe("Badge", () => {
  it("renders children and applies the given color", () => {
    renderWithProviders(<Badge color="#16A34A">Active</Badge>);
    const badge = screen.getByText("Active");
    expect(badge).toBeInTheDocument();
    expect(badge).toHaveStyleRule("color", "#16A34A");
  });
});

describe("OrderStatusBadge", () => {
  const cases: { status: OrderStatus; label: string; color: string }[] = [
    { status: "Pending",               label: "Pending",                color: "#F59E0B" },
    { status: "AwaitingPayment",       label: "Awaiting Payment",       color: "#3B82F6" },
    { status: "PaymentConfirmed",      label: "Payment Confirmed",      color: "#10B981" },
    { status: "Processing",            label: "Processing",             color: "#8B5CF6" },
    { status: "Shipped",               label: "Shipped",                color: "#06B6D4" },
    { status: "Delivered",             label: "Delivered",              color: "#16A34A" },
    { status: "CancellationRequested", label: "Cancellation Requested", color: "#F97316" },
    { status: "Refunding",             label: "Refunding",              color: "#EF4444" },
    { status: "Cancelled",             label: "Cancelled",              color: "#6B7280" },
  ];

  it.each(cases)(
    "renders $status with label '$label' and color $color",
    ({ status, label, color }) => {
      renderWithProviders(<OrderStatusBadge status={status} />);
      const badge = screen.getByText(label);
      expect(badge).toBeInTheDocument();
      expect(badge).toHaveStyleRule("color", color);
    }
  );
});

describe("PaymentProcessingBadge", () => {
  it("renders the optimistic settling label", () => {
    renderWithProviders(<PaymentProcessingBadge />);
    const badge = screen.getByRole("status", { name: "Payment processing" });
    expect(badge).toBeInTheDocument();
    expect(badge).toHaveTextContent("Payment Processing");
  });
});
