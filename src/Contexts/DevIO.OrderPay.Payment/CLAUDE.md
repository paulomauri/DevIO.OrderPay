# DevIO.OrderPay.Payment — Domain

Pure domain — no EF Core, no HTTP, no external dependencies.

- `Models/Payment.cs` — aggregate root + status state machine
  - flow: `Pending → Processing → Authorized → Captured → Refunded`; `Failed` is terminal
  - `BeginProcessing` / `Authorize(ref)` / `Capture` / `Decline` / `Abandon` / `Refund` — each validated against the allowed-transitions map; illegal moves throw `InvalidPaymentTransitionException`
  - rule: a **declined attempt does NOT fail the payment** — `Decline()` returns it to `Pending` (retryable); `Abandon()` (→ `Failed`) is the deliberate give-up
- `Models/PaymentStatus.cs` — enum + `PaymentStatusConverter`
- `Models/PaymentAttempt.cs` — one charge attempt; holds `IdempotencyKey` (`orderId:attemptNumber`), `Outcome`, `GatewayReference`; `NextNumber(existing)` computes the next attempt number; private parameterless ctor for EF
- `Models/PaymentAttemptOutcome.cs` — enum (`Pending → Succeeded | Failed`)
- `Models/Amount.cs` — value object (decimal + currency, defaults `USD`); throws `ValueLowerThanZeroException` on negative. Ctor param names must match property names (`value`/`currency`) for EF owned-type binding
- `Models/PaymentMethod.cs` — polymorphic: abstract `PaymentMethod` + `PaymentMethodCard` / `PaymentMethodACH`; `PaymentType` enum
- `Exceptions/InvalidPaymentTransitionException.cs` — thrown by the state machine
- `Exceptions/DuplicatePaymentAttemptException.cs` — thrown when the unique `IdempotencyKey` is violated
- `Exceptions/ValueLowerThanZeroException.cs` — thrown by `Amount`

## Idempotency

Charge is **at-most-once**: the `PaymentAttempt` is persisted (unique `IdempotencyKey`) **before** the gateway call; a retry with the same key returns the stored result instead of charging again. The same key across retries = idempotent; a new `AttemptNumber` = a deliberate fresh charge (only after a definitive decline).
