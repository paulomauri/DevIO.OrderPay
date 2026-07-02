namespace DevIO.OrderPay.Core.Gateway;

// Outbound payload for a ready-to-ship order. A self-contained wire contract (no domain
// entities) so the HTTP adapter serializes it directly. Built by the Application handler
// from the Order + its Customer's Address in Step 1.
//
// IdempotencyKey is the dedup key at the carrier — stable per order dispatch so an
// Outbox retry never creates a duplicate shipment.
public record LogisticsDispatch(
    Guid OrderId,
    string IdempotencyKey,
    DateTime CreatedAt,
    LogisticsAddress ShippingAddress,
    IReadOnlyList<LogisticsItem> Items);

public record LogisticsItem(Guid ProductId, int Quantity);

// Flattened shipping address mirroring the Customer Address value object.
public record LogisticsAddress(
    string PostalCode,
    string Street,
    string? Number,
    string? Complement,
    string? District,
    string City,
    string? State);
