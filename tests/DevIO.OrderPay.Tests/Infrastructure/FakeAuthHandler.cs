using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace DevIO.OrderPay.Tests.Infrastructure;

public class FakeAuthHandlerOptions : AuthenticationSchemeOptions
{
    public string[] Roles { get; set; } = [];
    public string UserId { get; set; } = "test-user-id";
    public string Email { get; set; } = "test@orderpay.com";
}

public class FakeAuthHandler : AuthenticationHandler<FakeAuthHandlerOptions>
{
    public FakeAuthHandler(
        IOptionsMonitor<FakeAuthHandlerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Options.UserId),
            new(ClaimTypes.Email, Options.Email)
        };

        foreach (var role in Options.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
