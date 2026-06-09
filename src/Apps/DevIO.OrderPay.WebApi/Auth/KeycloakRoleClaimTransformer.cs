// WebApi/Auth/KeycloakRoleClaimTransformer.cs
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace DevIO.OrderPay.WebApi.Auth;

public class KeycloakRoleClaimTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        // extract realm_access.roles from JWT
        var realmAccess = identity.FindFirst("realm_access");
        if (realmAccess is null) return Task.FromResult(principal);

        try
        {
            var realmAccessJson = JsonDocument.Parse(realmAccess.Value);
            if (!realmAccessJson.RootElement.TryGetProperty("roles", out var roles))
                return Task.FromResult(principal);

            foreach (var role in roles.EnumerateArray())
            {
                string? roleName = role.GetString();
                if (string.IsNullOrEmpty(roleName)) continue;

                // add as standard ClaimTypes.Role
                if (!identity.HasClaim(ClaimTypes.Role, roleName))
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
            }
        }
        catch (JsonException)
        {
            // ignore malformed claims
        }

        return Task.FromResult(principal);
    }
}