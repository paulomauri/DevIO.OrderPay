using FluentAssertions;
using NetArchTest.Rules;
using System.Reflection;

namespace DevIO.OrderPay.Tests.Architecture;

public class ArchitectureTests
{
    // ── Assembly references ───────────────────────────────────────────

    private static readonly Assembly CustomerDomain = typeof(DevIO.OrderPay.Customer.Models.Customer).Assembly;
    private static readonly Assembly OrderDomain = typeof(DevIO.OrderPay.Order.Models.Order).Assembly;
    private static readonly Assembly CustomerApp = typeof(DevIO.OrderPay.Customer.Application.DTOs.CustomerRequest).Assembly;
    private static readonly Assembly OrderApp = typeof(DevIO.OrderPay.Order.Application.Services.OrderService).Assembly;
    private static readonly Assembly Infra = typeof(DevIO.OrderPay.Infra.AppDbContext).Assembly;

    // ── Domain: no EF Core ────────────────────────────────────────────

    [Fact]
    public void CustomerDomain_ShouldNotReference_EntityFrameworkCore()
    {
        var result = Types.InAssembly(CustomerDomain)
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Customer domain must not depend on EF Core");
    }

    [Fact]
    public void OrderDomain_ShouldNotReference_EntityFrameworkCore()
    {
        var result = Types.InAssembly(OrderDomain)
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Order domain must not depend on EF Core");
    }

    // ── Domain: no ASP.NET Core ───────────────────────────────────────

    [Fact]
    public void CustomerDomain_ShouldNotReference_AspNetCore()
    {
        var result = Types.InAssembly(CustomerDomain)
            .Should()
            .NotHaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Customer domain must not depend on ASP.NET Core");
    }

    [Fact]
    public void OrderDomain_ShouldNotReference_AspNetCore()
    {
        var result = Types.InAssembly(OrderDomain)
            .Should()
            .NotHaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Order domain must not depend on ASP.NET Core");
    }

    // ── Domain: no Application ────────────────────────────────────────

    [Fact]
    public void CustomerDomain_ShouldNotReference_CustomerApplication()
    {
        var result = Types.InAssembly(CustomerDomain)
            .Should()
            .NotHaveDependencyOn("DevIO.OrderPay.Customer.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Customer domain must not reference its own application layer");
    }

    [Fact]
    public void OrderDomain_ShouldNotReference_OrderApplication()
    {
        var result = Types.InAssembly(OrderDomain)
            .Should()
            .NotHaveDependencyOn("DevIO.OrderPay.Order.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Order domain must not reference its own application layer");
    }

    // ── Application: no EF Core ───────────────────────────────────────

    [Fact]
    public void CustomerApplication_ShouldNotReference_EntityFrameworkCore()
    {
        var result = Types.InAssembly(CustomerApp)
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Customer application layer must not depend on EF Core");
    }

    [Fact]
    public void OrderApplication_ShouldNotReference_EntityFrameworkCore()
    {
        var result = Types.InAssembly(OrderApp)
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Order application layer must not depend on EF Core");
    }

    // ── Application: no ASP.NET Core ─────────────────────────────────

    [Fact]
    public void CustomerApplication_ShouldNotReference_AspNetCore()
    {
        var result = Types.InAssembly(CustomerApp)
            .Should()
            .NotHaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Customer application layer must not depend on ASP.NET Core");
    }

    [Fact]
    public void OrderApplication_ShouldNotReference_AspNetCore()
    {
        var result = Types.InAssembly(OrderApp)
            .Should()
            .NotHaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Order application layer must not depend on ASP.NET Core");
    }

    // ── Application: no Infrastructure ───────────────────────────────

    [Fact]
    public void CustomerApplication_ShouldNotReference_Infrastructure()
    {
        var result = Types.InAssembly(CustomerApp)
            .Should()
            .NotHaveDependencyOn("DevIO.OrderPay.Infra")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Customer application layer must not reference Infrastructure directly");
    }

    [Fact]
    public void OrderApplication_ShouldNotReference_Infrastructure()
    {
        var result = Types.InAssembly(OrderApp)
            .Should()
            .NotHaveDependencyOn("DevIO.OrderPay.Infra")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Order application layer must not reference Infrastructure directly");
    }

    // ── Infrastructure: no Application ───────────────────────────────

    [Fact]
    public void Infrastructure_ShouldNotReference_CustomerApplication()
    {
        var result = Types.InAssembly(Infra)
            .Should()
            .NotHaveDependencyOn("DevIO.OrderPay.Customer.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Infrastructure must not depend on the Customer application layer");
    }

    [Fact]
    public void Infrastructure_ShouldNotReference_OrderApplication()
    {
        var result = Types.InAssembly(Infra)
            .Should()
            .NotHaveDependencyOn("DevIO.OrderPay.Order.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Infrastructure must not depend on the Order application layer");
    }

    // ── Infrastructure: no WebApi ─────────────────────────────────────

    [Fact]
    public void Infrastructure_ShouldNotReference_WebApi()
    {
        var result = Types.InAssembly(Infra)
            .Should()
            .NotHaveDependencyOn("DevIO.OrderPay.WebApi")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Infrastructure must not depend on the WebApi layer");
    }

    // ── Naming conventions ────────────────────────────────────────────

    [Fact]
    public void Interfaces_ShouldBeNamedWithIPrefix()
    {
        var assemblies = new[] { CustomerDomain, OrderDomain, CustomerApp, OrderApp, Infra };

        foreach (var asm in assemblies)
        {
            var result = Types.InAssembly(asm)
                .That()
                .AreInterfaces()
                .Should()
                .HaveNameStartingWith("I")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                because: $"All interfaces in {asm.GetName().Name} must start with 'I'");
        }
    }

    [Fact]
    public void Repositories_ShouldResideInInfrastructure()
    {
        var result = Types.InAssembly(Infra)
            .That()
            .HaveNameEndingWith("Repository")
            .And()
            .AreNotInterfaces()
            .Should()
            .ResideInNamespace("DevIO.OrderPay.Infra.Repositories")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "All concrete repository implementations must live in the Infra.Repositories namespace");
    }

    [Fact]
    public void Services_ShouldResideInApplicationLayer()
    {
        var customerResult = Types.InAssembly(CustomerApp)
            .That()
            .HaveNameEndingWith("Service")
            .And()
            .AreNotInterfaces()
            .Should()
            .ResideInNamespace("DevIO.OrderPay.Customer.Application.Services")
            .GetResult();

        var orderResult = Types.InAssembly(OrderApp)
            .That()
            .HaveNameEndingWith("Service")
            .And()
            .AreNotInterfaces()
            .Should()
            .ResideInNamespace("DevIO.OrderPay.Order.Application.Services")
            .GetResult();

        customerResult.IsSuccessful.Should().BeTrue(
            because: "Customer services must live in Customer.Application.Services");
        orderResult.IsSuccessful.Should().BeTrue(
            because: "Order services must live in Order.Application.Services");
    }

    [Fact]
    public void Validators_ShouldResideInApplicationLayer()
    {
        var customerResult = Types.InAssembly(CustomerApp)
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .ResideInNamespace("DevIO.OrderPay.Customer.Application.Validators")
            .GetResult();

        var orderResult = Types.InAssembly(OrderApp)
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .ResideInNamespace("DevIO.OrderPay.Order.Application.Validators")
            .GetResult();

        customerResult.IsSuccessful.Should().BeTrue(
            because: "Customer validators must live in Customer.Application.Validators");
        orderResult.IsSuccessful.Should().BeTrue(
            because: "Order validators must live in Order.Application.Validators");
    }

    [Fact]
    public void DomainExceptions_ShouldResideInDomainLayer()
    {
        var customerResult = Types.InAssembly(CustomerDomain)
            .That()
            .HaveNameEndingWith("Exception")
            .Should()
            .ResideInNamespace("DevIO.OrderPay.Customer.Exceptions")
            .GetResult();

        var orderResult = Types.InAssembly(OrderDomain)
            .That()
            .HaveNameEndingWith("Exception")
            .Should()
            .ResideInNamespace("DevIO.OrderPay.Order.Exceptions")
            .GetResult();

        customerResult.IsSuccessful.Should().BeTrue(
            because: "Customer domain exceptions must live in Customer.Exceptions");
        orderResult.IsSuccessful.Should().BeTrue(
            because: "Order domain exceptions must live in Order.Exceptions");
    }
}
