# DevIO.OrderPay.Payment.Application

Application layer — services, validators, DTOs. No EF Core, no HTTP.

- `Services/IPaymentService.cs` + `PaymentService.cs` — orchestrates the idempotent flow:
  find attempt by key → if already resolved, replay the stored result → otherwise persist the
  attempt (+ a new `Payment`) **before** the gateway call → record the outcome → drive the
  state machine (`BeginProcessing → Authorize → Capture`, or `Decline`) → raise `PaymentCapturedEvent`
- `DTOs/PaymentRequest.cs` + `PaymentResponse.cs`
- `Validators/PaymentRequestValidator.cs` — FluentValidation; runs automatically before the controller action

### Payment → Order seam (Phase 8)

`Capture()` raises **`PaymentCapturedEvent`** (in `SharedKernel/Contracts`, so Payment doesn't
reference Order). `PaymentService` no longer makes any in-process call to the Order context — the
event is written to the Outbox in the same transaction as the payment save and dispatched to
RabbitMQ by the `OutboxWorker`; the Order-side consumer advances the order. The Phase 7
`Integration/PaymentCapturedEvent.cs` + `IPaymentCapturedHandler.cs` seam was **deleted**.

## Notes

- The gateway port `IPaymentGateway` + `PaymentGatewayResult` live in **`Core/Gateway`** (like the
  repository interfaces), so the Infra adapter (`MockPaymentGateway`) implements them without
  Infrastructure depending on this Application layer.
- `IPaymentRepository` (in Core) exposes the attempt lookups (`GetAttemptByKeyAsync`,
  `GetAttemptsByOrderIdAsync`, `AddAttemptAsync`) the idempotent flow relies on.
