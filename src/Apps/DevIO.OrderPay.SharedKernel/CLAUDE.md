# DevIO.OrderPay.SharedKernel

Dependency-free building blocks for the domain-events + Outbox machinery (Phase 8). Every domain,
`Infra`, and `WebApi` references this; it references **nothing** — which is the whole point.

- `Events/IDomainEvent.cs` — `Guid EventId` + `DateTime OccurredOn`. `EventId` is the dedup key that
  flows through the whole pipeline (`= OutboxMessage.Id = ProcessedOutboxMessage.Id`).
- `Events/DomainEvent.cs` — `abstract record DomainEvent : IDomainEvent` that stamps `EventId`
  (new `Guid`) + `OccurredOn` (UTC now). Concrete events are one-liners:
  `record OrderProcessingEvent(Guid OrderId) : DomainEvent;`
- `Events/IDomainEventHandler.cs` — `IDomainEventHandler<in TEvent>` with `HandleAsync`. The business
  reaction to an event; invoked by the MassTransit consumers in `WebApi/Messaging`.
- `AggregateRoot.cs` — base class that accumulates events in a private list, exposes them via
  `DomainEvents`, and offers `RaiseEvent` (protected) + `ClearDomainEvents`. `Order` and `Payment`
  inherit it; the `ConvertDomainEventsToOutboxInterceptor` reads `DomainEvents` at `SaveChanges` and
  persists them in the same transaction, then clears them.
- `Contracts/PaymentCapturedEvent.cs` — the **cross-context integration event** (Payment → Order).
  Placed here so `Payment` doesn't reference `Order` and vice-versa — they share only this contract.

## Why here and not Core

`Core` references the domain projects (for the repository/gateway interfaces), so putting
`IDomainEvent` there would make the domains → Core → domains cycle. SharedKernel has no references,
so domains can depend on it freely. See the root `CLAUDE.md` → "Phase 8".
