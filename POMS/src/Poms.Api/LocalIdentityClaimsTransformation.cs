using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Poms.Api;

public sealed class LocalIdentityClaimsTransformation(
    UserManager<IdentityUser> users,
    ILogger<LocalIdentityClaimsTransformation> logger) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated ||
            identity.HasClaim(claim => claim.Type == ClaimTypes.NameIdentifier))
            return principal;

        var email = principal.FindFirstValue("email") ?? principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning("Authenticated OIDC subject {Subject} has no email claim.", principal.FindFirstValue("sub"));
            return principal;
        }

        var localUser = await users.FindByEmailAsync(email);
        if (localUser is null || await users.IsLockedOutAsync(localUser))
        {
            logger.LogWarning("Authenticated OIDC email {Email} has no active local staff account.", email);
            return principal;
        }

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, localUser.Id));
        identity.AddClaim(new Claim(ClaimTypes.Email, localUser.Email ?? email));
        foreach (var role in await users.GetRolesAsync(localUser))
            identity.AddClaim(new Claim("poms_role", role));

        return principal;
    }
}

