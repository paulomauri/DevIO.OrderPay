# DevIO.OrderPay.Customer — Domain

Pure domain — no EF Core, no HTTP, no external dependencies.

- `Models/Customer.cs` — aggregate root (Id, Name, CPF, Email, Address, CreatedAt)
- `Models/Email.cs` — value object; validates format on construction
- `Models/Address.cs` — value object (Street, City, State, ZipCode)
- `Models/State.cs` — enum of Brazilian states
- `Exceptions/DuplicateCpfException.cs` — thrown by CustomerService
