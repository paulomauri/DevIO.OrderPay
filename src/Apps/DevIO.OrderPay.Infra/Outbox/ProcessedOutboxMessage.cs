namespace DevIO.OrderPay.Infra.Outbox;

// Records an OutboxMessage Id that a consumer has already handled, so a re-delivered
// message (at-least-once) is processed at-most-once → effectively-once.
public class ProcessedOutboxMessage
{
    public Guid Id { get; set; }
    public DateTime ProcessedOn { get; set; }
}
