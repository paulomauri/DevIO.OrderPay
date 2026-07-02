using DevIO.OrderPay.Infra;
using DevIO.OrderPay.WebApi.Extensions;
using DevIO.OrderPay.WebApi.Outbox;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevIO.OrderPay.Tests.Infrastructure;

public class OrderPayWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // MassTransit uses its in-memory transport in tests — no RabbitMQ broker required.
        builder.UseSetting("Messaging:Transport", "InMemory");

        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<RateLimiterSettings>(o => o.Enabled = false);

            // No integration test asserts async Outbox draining, and each test spins up its
            // own host via WithWebHostBuilder — leaving the 1s-poll BackgroundService running
            // makes those hosts race a disposed bus/DbContext on teardown (intermittent
            // NullReferenceException at class cleanup). Drop it in tests.
            var outboxWorker = services.SingleOrDefault(
                d => d.ImplementationType == typeof(OutboxWorker));
            if (outboxWorker is not null) services.Remove(outboxWorker);
            // Remove DbContextOptions<AppDbContext>
            var optionsDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (optionsDesc is not null) services.Remove(optionsDesc);

            // IDbContextOptionsConfiguration<AppDbContext> is an internal EF Core type that
            // stores the UseSqlServer() call. Both it and the InMemory config would run
            // together causing a dual-provider error. Remove it via reflection.
            var configServiceType = services
                .Select(d => d.ServiceType)
                .FirstOrDefault(t => t.IsGenericType &&
                                     t.GetGenericTypeDefinition().Name
                                      .StartsWith("IDbContextOptionsConfiguration") &&
                                     t.GetGenericArguments()[0] == typeof(AppDbContext));

            if (configServiceType is not null)
            {
                var toRemove = services.Where(d => d.ServiceType == configServiceType).ToList();
                foreach (var d in toRemove) services.Remove(d);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("IntegrationTestDb"));
        });
    }

    // Creates an authenticated client with the given roles.
    public HttpClient CreateClientWithRoles(params string[] roles)
    {
        return WithWebHostBuilder(b => b.ConfigureTestServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<FakeAuthHandlerOptions, FakeAuthHandler>("Test", opts =>
                    opts.Roles = roles);

            services.PostConfigure<AuthenticationOptions>(opts =>
            {
                opts.DefaultAuthenticateScheme = "Test";
                opts.DefaultChallengeScheme = "Test";
                opts.DefaultForbidScheme = "Test";
            });
        })).CreateClient();
    }
}
