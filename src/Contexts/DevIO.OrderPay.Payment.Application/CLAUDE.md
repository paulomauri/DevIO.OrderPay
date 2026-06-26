# DevIO.OrderPay.Payment.Application

Application layer — services, validators, DTOs. No EF Core, no HTTP.

- `Services/IPaymentService.cs` + `PaymentService.cs` — orchestrates the idempotent flow:
  find attempt by key → if already resolved, replay the stored result → otherwise persist the
  attempt (+ a new `Payment`) **before** the gateway call → record the outcome → drive the
  state machine (`BeginProcessing → Authorize → Capture`, or `Decline`) → raise `PaymentCapturedEvent`
- `DTOs/PaymentRequest.cs` + `PaymentResponse.cs`
- `Validators/PaymentRequestValidator.cs` — FluentValidation; runs automatically before the controller action
- `Integration/PaymentCapturedEvent.cs` + `IPaymentCapturedHandler.cs` — the seam to the Order
  context. `PaymentService` calls the handler after a capture; the WebApi composition root provides
  an implementation that advances the order (keeping the contexts decoupled). Phase 8 swaps this
  in-process dispatch for the Outbox.

## Notes

- The gateway port `IPaymentGateway` + `PaymentGatewayResult` live in **`Core/Gateway`** (like the
  repository interfaces), so the Infra adapter (`MockPaymentGateway`) implements them without
  Infrastructure depending on this Application layer.
- `IPaymentRepository` (in Core) exposes the attempt lookups (`GetAttemptByKeyAsync`,
  `GetAttemptsByOrderIdAsync`, `AddAttemptAsync`) the idempotent flow relies on.
