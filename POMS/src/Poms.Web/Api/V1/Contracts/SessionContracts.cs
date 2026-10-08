namespace Poms.Web.Api.V1.Contracts;

public sealed record SessionResponse(
    string UserId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles);

