using DevIO.OrderPay.Payment.Models;

namespace DevIO.OrderPay.Payment.Application.DTOs;

public class PaymentRequest
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    // Card details (Phase 7 supports card payments; ACH can be added later).
    public PaymentType Type { get; set; } = PaymentType.CREDIT;
    public string CardBrand { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public string Expiry { get; set; } = string.Empty;

    // Optional — reusing the same attempt number makes a retry idempotent
    // (same key → at-most-once charge). Omit it to start a fresh attempt.
    public int? AttemptNumber { get; set; }
}
