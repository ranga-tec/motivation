using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;

namespace Poms.Web.Api;

public static class ApiAuthenticationDefaults
{
    public const string Scheme = "PomsApiAuthentication";
}

public static class ApiAuthenticationSchemeSelector
{
    public static string Select(HttpContext context, bool bearerAuthenticationEnabled)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        return bearerAuthenticationEnabled &&
            authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? JwtBearerDefaults.AuthenticationScheme
                : IdentityConstants.ApplicationScheme;
    }
}
