using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevIO.OrderPay.WebApi.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "Frontend";

    public static WebApplicationBuilder AddCorsPolicy(this WebApplicationBuilder builder)
    {
        string[] origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        builder.Services.AddCors(options =>
            options.AddPolicy(PolicyName, policy => policy
                .WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()));

        return builder;
    }
}
