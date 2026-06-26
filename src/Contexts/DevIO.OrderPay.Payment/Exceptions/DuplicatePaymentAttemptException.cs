namespace DevIO.OrderPay.Payment.Exceptions;

// Thrown when an attempt with the same IdempotencyKey already exists — i.e. the
// unique index rejected a concurrent or replayed charge. The guard that keeps a
// payment at-most-once.
public class DuplicatePaymentAttemptException(string idempotencyKey)
    : Exception($"A payment attempt with idempotency key '{idempotencyKey}' already exists.")
{
    public string IdempotencyKey { get; } = idempotencyKey;
}
