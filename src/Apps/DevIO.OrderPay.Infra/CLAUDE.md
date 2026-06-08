# DevIO.OrderPay.Infra

EF Core, DbContext, migrations, repository implementations.

- `AppDbContext.cs` — single DbContext for all bounded contexts; add `DbSet<T>` here for each new entity
- `Repositories/Repository.cs` — generic EF Core base
- `Repositories/CustomerRepository.cs` — implements ICustomerRepository
- `Repositories/OrderRepository.cs` — implements IOrderRepository
- `Repositories/ProductRepository.cs` — implements IProductRepository
- `Migrations/` — EF Core migration files

## Migrations

```bash
dotnet ef migrations add <Name> \
  --project src/Apps/DevIO.OrderPay.Infra \
  --startup-project src/Apps/DevIO.OrderPay.WebApi

dotnet ef database update \
  --project src/Apps/DevIO.OrderPay.Infra \
  --startup-project src/Apps/DevIO.OrderPay.WebApi
```
