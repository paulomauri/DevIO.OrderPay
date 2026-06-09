using DevIO.OrderPay.Customer.Application.Services;
using DevIO.OrderPay.Core.Repository;
using DevIO.OrderPay.Customer.Application.Validators;
using DevIO.OrderPay.Order.Application.Services;
using DevIO.OrderPay.Order.Application.Validators;
using DevIO.OrderPay.Infra;
using DevIO.OrderPay.Infra.Repositories;
using DevIO.OrderPay.WebApi.Auth;
using DevIO.OrderPay.WebApi.Extensions;
using DevIO.OrderPay.WebApi.Middleware;
using DevIO.OrderPay.WebApi.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ── Logging ────────────────────────────────────────────────
builder.AddSerilogLogging();

// ── OpenTelemetry ──────────────────────────────────────────
builder.AddOpenTelemetryObservability();

// ── Controllers + API ──────────────────────────────────────
builder.Services.AddControllers()
     .ConfigureApiBehaviorOptions(options =>
         // let FluentValidation handle validation responses
         options.SuppressModelStateInvalidFilter = true);
builder.Services.AddEndpointsApiExplorer();


// ── Swagger with JWT support ───────────────────────────────
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DevIO.OrderPay API",
        Version = "v1"
    });

    // JWT support — Swashbuckle 10.x syntax
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token here."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// ── API Versioning ─────────────────────────────────────────
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ── Authentication — Keycloak JWT ──────────────────────────
builder.AddKeycloakAuthentication();

// ── Rate Limiting ──────────────────────────────────────────
builder.AddRateLimiting();

// ── Role claim transformer ─────────────────────────────────
builder.Services.AddScoped<IClaimsTransformation, KeycloakRoleClaimTransformer>();

// ── Database ───────────────────────────────────────────────

// Add Migration
// Add-Migration "InitProject" -Project DevIO.OrderPay.Infra -StartupProject DevIO.OrderPay.WebApi
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// ── Validation ─────────────────────────────────────────────
// FluentValidation — scans Application assembly for validators
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CustomerRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CustomerInfoRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ProductRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<OrderRequestValidator>();

// ── IoC ────────────────────────────────────────────────────
// Bind interface (Core) → implementation (Infra)
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();

// ── CorrelationId Acessor ────────────────────────────────────────────────────-
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();

// ── Health Check ────────────────────────────────────────────────────-
builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

// apply migrations on startup
await app.InitializeDatabaseAsync();

// ── Middleware pipeline ────────────────────────────────────
app.UseSerilogRequestLogging(options => options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms");

// Apply Middleware
// CorrelationId MUST come before RequestLogging
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseSerilogRequestLogging();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "DevIO.OrderPay API v1");
    options.RoutePrefix = string.Empty;
});

// keep OpenApi only for development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map Health Check
app.MapHealthChecks("/health");

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }


/**

Client Request
    ↓
CorrelationIdMiddleware
    ├── reads  X-Correlation-Id header (if sent by client)
    └── or generates new Guid
    ↓
LogContext.PushProperty("CorrelationId", id)  ← attaches to ALL logs
    ↓
RequestLoggingMiddleware
    └── logs method, path, status, duration, correlationId
    ↓
Controller → Service → Repository
    └── every Log.Information() automatically includes CorrelationId
    ↓
Seq Dashboard
    └── filter: CorrelationId = '3f2a1b...'  ← see entire request trace

*/
