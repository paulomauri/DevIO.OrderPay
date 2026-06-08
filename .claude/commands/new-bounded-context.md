# /new-bounded-context

Create a complete new bounded context for this project following the Clean Architecture pattern established by the Customer context. The context name is: **$ARGUMENTS**

## Steps to execute in order

### 1. Read existing patterns first
Before writing any code, read these files to match the exact patterns in use:
- `src/Contexts/DevIO.OrderPay.Customer/Models/Customer.cs`
- `src/Contexts/DevIO.OrderPay.Customer.Application/Services/CustomerService.cs`
- `src/Contexts/DevIO.OrderPay.Customer.Application/Validators/CustomerRequestValidator.cs`
- `src/Apps/DevIO.OrderPay.Core/Repository/ICustomerRepository.cs`
- `src/Apps/DevIO.OrderPay.Infra/Repositories/CustomerRepository.cs`
- `src/Apps/DevIO.OrderPay.Infra/AppDbContext.cs`
- `src/Apps/DevIO.OrderPay.WebApi/Controllers/CustomerController.cs`
- `src/Apps/DevIO.OrderPay.WebApi/Program.cs`
- `src/Contexts/DevIO.OrderPay.Customer/DevIO.OrderPay.Customer.csproj`
- `src/Contexts/DevIO.OrderPay.Customer.Application/DevIO.OrderPay.Customer.Application.csproj`

### 2. Domain project — `src/Contexts/DevIO.OrderPay.<Context>/`

Create:
- `DevIO.OrderPay.<Context>.csproj` — same SDK/framework/refs as Customer, no EF Core
- `Models/<Entity>.cs` — aggregate root, no business rule enforcement in the model
- `Exceptions/Duplicate<Key>Exception.cs` — if the context has a unique key constraint
- `CLAUDE.md` — describe what this domain owns, its models, and its rules

### 3. Application project — `src/Contexts/DevIO.OrderPay.<Context>.Application/`

Create:
- `DevIO.OrderPay.<Context>.Application.csproj` — refs Domain + Core, no EF Core
- `DTOs/<Entity>Request.cs` and `<Entity>Response.cs`
- `Validators/<Entity>RequestValidator.cs` — FluentValidation rules
- `Services/I<Entity>Service.cs` — GetAll, GetById, AddAsync, UpdateAsync, DeleteAsync
- `Services/<Entity>Service.cs` — calls repository, enforces uniqueness rules, throws domain exceptions
- `CLAUDE.md` — describe what this application layer owns and its rules

### 4. Core — add repository interface

Add `I<Entity>Repository.cs` to `src/Apps/DevIO.OrderPay.Core/Repository/` extending `IRepository<<Entity>>`.
Include any domain-specific query methods (e.g. `ExistsByKeyAsync`).

### 5. Infrastructure — implement repository + register DbSet

- Add `<Entity>Repository.cs` to `src/Apps/DevIO.OrderPay.Infra/Repositories/` implementing `I<Entity>Repository`
- Add `DbSet<<Entity>> <Entities> { get; set; }` to `AppDbContext.cs`
- Add EF Core configuration (relationships, indexes, constraints) in `AppDbContext.OnModelCreating` if needed

### 6. WebApi — controller + DI registration

- Add `<Entity>Controller.cs` to `src/Apps/DevIO.OrderPay.WebApi/Controllers/`
  - `[Authorize(Roles = "admin")]` on write endpoints (POST, PUT, DELETE)
  - No role restriction on GET endpoints
  - Catch domain exceptions → return appropriate HTTP status codes (409, 404)
- Register in `Program.cs`: `builder.Services.AddScoped<I<Entity>Service, <Entity>Service>()` and `builder.Services.AddScoped<I<Entity>Repository, <Entity>Repository>()`

### 7. Database migration

```bash
dotnet ef migrations add Add<Context> \
  --project src/Apps/DevIO.OrderPay.Infra \
  --startup-project src/Apps/DevIO.OrderPay.WebApi

dotnet ef database update \
  --project src/Apps/DevIO.OrderPay.Infra \
  --startup-project src/Apps/DevIO.OrderPay.WebApi
```

### 8. Tests

Add test classes to `tests/DevIO.OrderPay.Tests/`:
- `Domain/<Entity>Tests.cs` — value object validation (if any)
- `Application/<Entity>RequestValidatorTests.cs` — FluentValidation rules
- `Application/<Entity>ServiceTests.cs` — mock IRepository, test happy/unhappy paths
- `WebApi/<Entity>ControllerTests.cs` — unit test controller with mocked service
- `WebApi/<Entity>ControllerIntegrationTests.cs` — WebApplicationFactory + InMemory EF

### 9. Solution file

Add new folders and files to `DevIO.OrderPay.slnx`:
```xml
<Folder Name="/src/Contexts/<Context>/">
  <File Path="src/Contexts/DevIO.OrderPay.<Context>/CLAUDE.md" />
  <File Path="src/Contexts/DevIO.OrderPay.<Context>.Application/CLAUDE.md" />
  <Project Path="src/Contexts/DevIO.OrderPay.<Context>/DevIO.OrderPay.<Context>.csproj" />
  <Project Path="src/Contexts/DevIO.OrderPay.<Context>.Application/DevIO.OrderPay.<Context>.Application.csproj" />
</Folder>
```

## Rules to follow

- Do NOT add business logic to domain models
- Do NOT reference EF Core from Domain or Application projects
- Do NOT skip the CLAUDE.md files — they reduce token consumption in future sessions
- Match the exact file/folder structure of the Customer context
- Run `dotnet build` after all files are created to verify — fix any errors before stopping
- Run `dotnet test` after tests are written — all must pass before stopping
