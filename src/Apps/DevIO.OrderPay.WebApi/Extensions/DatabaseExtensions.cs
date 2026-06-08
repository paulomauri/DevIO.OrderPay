using DevIO.OrderPay.Infra;
using Microsoft.EntityFrameworkCore;

// DevIO.OrderPay.WebApi/Extensions/DatabaseExtensions.cs
namespace DevIO.OrderPay.WebApi.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();

        AppDbContext context = scope.ServiceProvider
                                    .GetRequiredService<AppDbContext>();

        if (context.Database.IsRelational())
            await context.Database.MigrateAsync();
        else
            await context.Database.EnsureCreatedAsync();
    }
}