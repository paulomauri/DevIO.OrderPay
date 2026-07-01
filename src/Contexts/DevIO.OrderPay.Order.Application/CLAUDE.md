# DevIO.OrderPay.Order.Application

Application layer — services, validators, DTOs. No EF Core, no HTTP.

- `Services/IOrderService.cs` + `OrderService.cs` — GetAll, GetById, GetByCustomerId, AddAsync, DeleteAsync, UpdateStatusAsync, UpdateDeliveryDateAsync, AddItemAsync, RemoveItemAsync
  - `DuplicateOrderItemException` and `ValueLowerThanZeroException` propagate to controller
  - TotalPrice/TotalDiscount recalculated on every item add/remove
- `Services/IProductService.cs` + `ProductService.cs` — CRUD + UpdateSkuAsync
- `EventHandlers/OrderEventHandlers.cs` (Phase 8) — `IDomainEventHandler<T>` business logic invoked by the WebApi MassTransit consumers: `ConfirmOrderOnPaymentCaptured` (`PaymentCapturedEvent` → `PaymentConfirmed`) and `StartProcessingOnOrderConfirmed` (`PaymentConfirmedEvent` → `Processing`). Keeps the advance logic out of WebApi/Infra.
- `Validators/OrderRequestValidator.cs` — CustomerId required, Items not empty, Quantity > 0, Discount ≤ Price
- `Validators/ProductRequestValidator.cs` — Name/SKU/Description required, SKU alphanumeric
- `DTOs/OrderRequest.cs`, `OrderResponse.cs`, `OrderItemRequest.cs`, `OrderItemResponse.cs`
- `DTOs/ProductRequest.cs`, `ProductResponse.cs`
