# DevIO.OrderPay.Core

Shared abstractions — no business logic, no EF Core, no dependencies on other layers.

- `Repository/IRepository.cs` — generic CRUD interface
- `Repository/ICustomerRepository.cs` — adds `GetByEmailAsync`, `GetByCpfAsync`
- `Repository/IOrderRepository.cs` — adds `UpdateStatusAsync`, `UpdateDeliveryDateAsync`, `GetByCustomerIdAsync`
- `Repository/IProductRepository.cs` — adds `UpdateSkuAsync`

New bounded contexts add their own `I<Entity>Repository` here. Never add concrete implementations.
