using DevIO.OrderPay.Infra;
using DevIO.OrderPay.Order.Application.DTOs;
using DevIO.OrderPay.Tests.Infrastructure;
using DevIO.OrderPay.WebApi.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace DevIO.OrderPay.Tests.WebApi;

public class RateLimitingIntegrationTests
{
    [Fact]
    public async Task GeneralPolicy_AfterPermitLimitExceeded_Returns429()
    {
        using RateLimitingWebApplicationFactory factory = new(generalLimit: 3);
        HttpClient client = factory.CreateClientWithRoles("admin");

        for (int i = 0; i < 3; i++)
        {
            HttpResponseMessage response = await client.GetAsync("/api/v1/product");
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        HttpResponseMessage limited = await client.GetAsync("/api/v1/product");
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task WritesPolicy_AfterPermitLimitExceeded_Returns429()
    {
        using RateLimitingWebApplicationFactory factory = new(writesLimit: 2);
        HttpClient client = factory.CreateClientWithRoles("admin");
        ProductRequest product = new() { Name = "Test", SKU = "SKU001", Description = "desc" };

        for (int i = 0; i < 2; i++)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/product", product);
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        HttpResponseMessage limited = await client.PostAsJsonAsync("/api/v1/product", product);
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GeneralPolicy_RejectedResponse_IncludesRetryAfterHeader()
    {
        using RateLimitingWebApplicationFactory factory = new(generalLimit: 1);
        HttpClient client = factory.CreateClientWithRoles("admin");

        await client.GetAsync("/api/v1/product");

        HttpResponseMessage limited = await client.GetAsync("/api/v1/product");
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        limited.Headers.Should().ContainKey("Retry-After");
    }

    [Fact]
    public async Task WritesPolicy_DoesNotThrottle_ReadRequests()
    {
        // Write limit is 1 — exhaust it, then verify reads still work on GeneralPolicy (limit=10)
        using RateLimitingWebApplicationFactory factory = new(generalLimit: 10, writesLimit: 1);
        HttpClient client = factory.CreateClientWithRoles("admin");
        ProductRequest product = new() { Name = "Test", SKU = "SKU002", Description = "desc" };

        await client.PostAsJsonAsync("/api/v1/product", product);

        HttpResponseMessage response = await client.GetAsync("/api/v1/product");
        response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }
}

// Standalone factory — does NOT inherit from OrderPayWebApplicationFactory to avoid
// the PostConfigure(Enabled=false) that factory injects for other integration tests.
internal sealed class RateLimitingWebApplicationFactory(int generalLimit = 100, int writesLimit = 20)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<RateLimiterSettings>(o =>
            {
                o.GenLimit = generalLimit;
                o.WrtLimit = writesLimit;
            });

            var optionsDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (optionsDesc is not null) services.Remove(optionsDesc);

            var configServiceType = services
                .Select(d => d.ServiceType)
                .FirstOrDefault(t => t.IsGenericType &&
                                     t.GetGenericTypeDefinition().Name
                                      .StartsWith("IDbContextOptionsConfiguration") &&
                                     t.GetGenericArguments()[0] == typeof(AppDbContext));

            if (configServiceType is not null)
            {
                List<ServiceDescriptor> toRemove = [.. services.Where(d => d.ServiceType == configServiceType)];
                foreach (var d in toRemove) services.Remove(d);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("RateLimitingTestDb"));
        });
    }

    public HttpClient CreateClientWithRoles(params string[] roles) =>
        WithWebHostBuilder(b => b.ConfigureTestServices(services =>
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
