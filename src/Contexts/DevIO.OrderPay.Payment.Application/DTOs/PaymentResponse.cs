namespace DevIO.OrderPay.Payment.Application.DTOs;

public class PaymentResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? GatewayReference { get; set; }
    public int AttemptNumber { get; set; }
    public string AttemptOutcome { get; set; } = string.Empty;
}
