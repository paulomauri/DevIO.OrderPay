# DevIO.OrderPay.Order — Domain

Pure domain — no EF Core, no HTTP, no external dependencies.

- `Models/Order.cs` — aggregate root (CustomerId, Details, TotalPrice, TotalDiscount, DeliveryDate, Status, Items, CreatedAt, UpdatedAt)
  - `AddItem(productId, quantity, price, discount)` — throws `DuplicateOrderItemException` if same product added twice
  - `RemoveItem(item)` — requires at least 1 item to remain
- `Models/OrderItem.cs` — child entity (OrderId, ProductId, Quantity, Price, Discount)
- `Models/Product.cs` — (Id, Name, SKU, Description, CreatedAt, UpdatedAt)
- `Models/Price.cs` — value object; throws `ValueLowerThanZeroException` on negative value
- `Models/OrderStatus.cs` — enum (Pending → AwaitingPayment → PaymentConfirmed → Processing → Shipped → Delivered; cancellation: CancellationRequested → Refunding → Cancelled) + `OrderStatusConverter`
- `Models/Customer.cs` — bounded context reference (CustomerId, Name); not stored in DB, ignored by EF
- `Exceptions/DuplicateOrderItemException.cs` — thrown by `Order.AddItem`
- `Exceptions/ValueLowerThanZeroException.cs` — thrown by `Price` constructor
