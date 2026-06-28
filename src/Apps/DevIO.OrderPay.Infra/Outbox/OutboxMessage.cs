namespace DevIO.OrderPay.Infra.Outbox;

// One domain event, persisted in the SAME transaction as the aggregate that raised it
// (written by ConvertDomainEventsToOutboxInterceptor). The OutboxWorker later dispatches it.
public class OutboxMessage
{
    public Guid Id { get; set; }              // = the domain event's EventId — the dedup key
    public string Type { get; set; } = "";    // assembly-qualified CLR type, for deserialization
    public string Content { get; set; } = ""; // JSON payload
    public DateTime OccurredOn { get; set; }
    public DateTime? ProcessedOn { get; set; } // null = not yet dispatched
    public string? Error { get; set; }
}
