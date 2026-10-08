using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Poms.Web.Api;
using Poms.Web.Api.V1.Contracts;

namespace Poms.Web.Api.V1.Controllers;

[ApiController]
[Route("api/v1/session")]
[Authorize(AuthenticationSchemes = ApiAuthenticationDefaults.Scheme, Policy = "AnyAuthenticatedUser")]
public sealed class SessionApiController : ControllerBase
{
    [HttpGet]
    public ActionResult<SessionResponse> Get()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email") ?? "";
        var displayName = User.Identity?.Name ?? email;
        var roles = User.FindAll("poms_role").Select(claim => claim.Value).Distinct().OrderBy(role => role).ToList();
        return Ok(new SessionResponse(userId, email, displayName, roles));
    }
}

