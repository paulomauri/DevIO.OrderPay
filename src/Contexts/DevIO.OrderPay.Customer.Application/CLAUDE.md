# DevIO.OrderPay.Customer.Application

Application layer — services, validators, DTOs. No EF Core, no HTTP.

- `Services/ICustomerService.cs` + `CustomerService.cs` — CRUD; throws `DuplicateCpfException` on duplicate CPF
- `Validators/CustomerRequestValidator.cs` — FluentValidation; runs automatically before controller action
- `DTOs/CustomerRequest.cs` + `CustomerResponse.cs`

CPF uniqueness checked via `ICustomerRepository.ExistsByCpfAsync` — not in the domain model.
