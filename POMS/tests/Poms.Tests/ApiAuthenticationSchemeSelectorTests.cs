using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Poms.Web.Api;

namespace Poms.Tests;

public sealed class ApiAuthenticationSchemeSelectorTests
{
    [Fact]
    public void Select_UsesBearerForAuthorizationHeader_WhenEnabled()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer test-token";

        var scheme = ApiAuthenticationSchemeSelector.Select(context, bearerAuthenticationEnabled: true);

        scheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
    }

    [Fact]
    public void Select_UsesIdentityCookieWithoutBearerHeader()
    {
        var context = new DefaultHttpContext();

        var scheme = ApiAuthenticationSchemeSelector.Select(context, bearerAuthenticationEnabled: true);

        scheme.Should().Be(IdentityConstants.ApplicationScheme);
    }

    [Fact]
    public void Select_UsesIdentityCookieWhenBearerAuthenticationIsDisabled()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer test-token";

        var scheme = ApiAuthenticationSchemeSelector.Select(context, bearerAuthenticationEnabled: false);

        scheme.Should().Be(IdentityConstants.ApplicationScheme);
    }
}
