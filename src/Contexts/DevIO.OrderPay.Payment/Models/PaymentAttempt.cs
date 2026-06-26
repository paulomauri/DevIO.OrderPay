using DevIO.OrderPay.Payment.Models;

public class PaymentAttempt
{
    public Guid Id { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public Guid OrderId { get; private set; }
    public int AttemptNumber { get; private set; }
    public PaymentAttemptOutcome Outcome { get; private set; }
    public string? GatewayReference { get; private set; }   // null until the gateway responds
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    // EF Core materializes through this parameterless ctor, writing the read-only
    // properties via their backing fields.
    private PaymentAttempt() { }

    public PaymentAttempt(Guid orderId, int attemptNumber)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        AttemptNumber = attemptNumber;
        IdempotencyKey = $"{orderId}:{attemptNumber}";   // derive the key — always consistent
        Outcome = PaymentAttemptOutcome.Pending;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = CreatedAt.AddMinutes(30);
    }

    public void RecordSuccess(string gatewayReference)
    {
        Outcome = PaymentAttemptOutcome.Succeeded;
        GatewayReference = gatewayReference;
    }

    public void RecordFailure() => Outcome = PaymentAttemptOutcome.Failed;

    // The next attempt number for an order, given the attempts already on record.
    // Max+1 (not Count+1) is safe even if an attempt row was ever removed.
    public static int NextNumber(IEnumerable<PaymentAttempt> existing)
    {
        var someExisting = existing.ToList();
        return someExisting.Count > 0 ? someExisting.Max(a => a.AttemptNumber) + 1 : 1;
    }
}
