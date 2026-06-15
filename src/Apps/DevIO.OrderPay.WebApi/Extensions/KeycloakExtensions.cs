using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using DevIO.OrderPay.WebApi.Resilience;
using Polly;
using System.Security.Claims;

namespace DevIO.OrderPay.WebApi.Extensions;

public static class KeycloakExtensions
{
    public static WebApplicationBuilder AddKeycloakAuthentication(
        this WebApplicationBuilder builder)
    {
        string? authority        = builder.Configuration["Keycloak:Authority"];
        string? audience         = builder.Configuration["Keycloak:Audience"];
        string? metadataAddress  = builder.Configuration["Keycloak:MetadataAddress"];
        string? validIssuer      = builder.Configuration["Keycloak:ValidIssuer"];
        bool requireHttps     = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority            = authority;
                options.Audience             = audience;
                options.RequireHttpsMetadata = requireHttps;

                if (!string.IsNullOrEmpty(metadataAddress))
                {
                    options.MetadataAddress = metadataAddress;

                    string publicBase   = new Uri(authority!).GetLeftPart(UriPartial.Authority);
                    string internalBase = new Uri(metadataAddress).GetLeftPart(UriPartial.Authority);

                    // Chain: PollyResilienceHandler → HostRewritingHandler → HttpClientHandler
                    // Polly retries the full request (including URL rewrite) on transient failures.
                    var pipeline     = ResiliencePipelineFactory.CreateKeycloakPipeline();
                    var hostRewriter = new HostRewritingHandler(publicBase, internalBase, new HttpClientHandler());
                    options.BackchannelHttpHandler = new PollyResilienceHandler(pipeline, hostRewriter);
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidIssuer              = validIssuer,
                    ValidateAudience         = true,
                    ValidAudience            = audience,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    RoleClaimType            = ClaimTypes.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        logger.LogError("Authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        logger.LogInformation("Token validated for {User}",
                            context.Principal?.Identity?.Name);
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly",       policy => policy.RequireRole("admin"));
            options.AddPolicy("CustomerOnly",    policy => policy.RequireRole("customer"));
            options.AddPolicy("AdminOrCustomer", policy => policy.RequireRole("admin", "customer"));
        });

        return builder;
    }

}

// Rewrites the public Keycloak hostname to the internal Docker/K8s hostname in every
// backchannel HTTP request the JwtBearer middleware makes (discovery doc + JWKS).
internal sealed class HostRewritingHandler(string publicBase, string internalBase, HttpMessageHandler innerHandler)
    : DelegatingHandler(innerHandler)
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null)
        {
            string rewritten = request.RequestUri.ToString()
                .Replace(publicBase, internalBase, StringComparison.OrdinalIgnoreCase);
            request.RequestUri = new Uri(rewritten);
        }
        return base.SendAsync(request, cancellationToken);
    }
}

// Wraps any DelegatingHandler chain with a Polly resilience pipeline.
// Sits at the outermost position so retries re-enter the full handler chain.
internal sealed class PollyResilienceHandler(
    ResiliencePipeline<HttpResponseMessage> pipeline,
    HttpMessageHandler innerHandler)
    : DelegatingHandler(innerHandler)
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => await pipeline.ExecuteAsync(
            ct => new ValueTask<HttpResponseMessage>(base.SendAsync(request, ct)),
            cancellationToken);
}
