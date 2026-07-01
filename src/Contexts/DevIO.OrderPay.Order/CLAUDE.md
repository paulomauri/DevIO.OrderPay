# DevIO.OrderPay.Order — Domain

Pure domain — no EF Core, no HTTP. References `SharedKernel` only (for `AggregateRoot` / events).

- `Models/Order.cs` — **`Order : AggregateRoot`** (CustomerId, Details, TotalPrice, TotalDiscount, DeliveryDate, Status, Items, DeliveredAt, DeliveredBy, CreatedAt, UpdatedAt)
  - `AddItem(productId, quantity, price, discount)` — throws `DuplicateOrderItemException` if same product added twice
  - `RemoveItem(item)` — requires at least 1 item to remain
- `Models/OrderItem.cs` — child entity (OrderId, ProductId, Quantity, Price, Discount)
- `Models/Product.cs` — (Id, Name, SKU, Description, CreatedAt, UpdatedAt)
- `Models/Price.cs` — value object; throws `ValueLowerThanZeroException` on negative value
- `Models/OrderStatus.cs` — enum (Pending → AwaitingPayment → PaymentConfirmed → Processing → Shipped → Delivered; cancellation: CancellationRequested → Refunding → Cancelled) + `OrderStatusConverter`. **No `Failed` state.**
- `Models/Customer.cs` — bounded context reference (CustomerId, Name); not stored in DB, ignored by EF
- `Events/OrderEvents.cs` — `PaymentConfirmedEvent`, `OrderProcessingEvent`, `OrderShippedEvent`, `OrderDeliveredEvent`, `OrderCancelledEvent` (each `: DomainEvent`, carries `OrderId`)
- `Exceptions/DuplicateOrderItemException.cs` — thrown by `Order.AddItem`
- `Exceptions/InvalidOrderTransitionException.cs` — thrown by `UpdateStatus` on an illegal move (→ `422` in the controller)
- `Exceptions/ValueLowerThanZeroException.cs` — thrown by `Price` constructor

## State machine (Phase 8)

`Status` is `{ get; private set; }` — the aggregate is the only mutator.

- `UpdateStatus(next)` — **no-ops if `Status == next`** (redelivered event is safe), validates `next`
  against the `_allowedTransitions` map, sets `Status` + `UpdatedAt`, then raises the matching event.
  `Pending → PaymentConfirmed` is allowed (payment advances the order straight from Pending).
- `MarkDelivered(deliveredAt, deliveredBy)` — `UpdateStatus(Delivered)` then records the two fields
  (`DeliveredBy` = carrier/driver/notes). Used by the Phase 9 logistics webhook.
- Events accumulate in `AggregateRoot.DomainEvents`; the Infra Outbox interceptor persists and clears
  them on `SaveChanges`. See the root `CLAUDE.md` → "Phase 8".
