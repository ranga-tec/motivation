using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;
using Poms.Web.Api;
using Poms.Web.Api.V1.Contracts;

namespace Poms.Web.Api.V1.Controllers;

[ApiController, Route("api/v1/dashboard")]
[Authorize(AuthenticationSchemes = ApiAuthenticationDefaults.Scheme, Policy = "AnyAuthenticatedUser")]
public sealed class DashboardApiController(PomsDbContext context, IRestrictedAccessService restrictedAccess) : ControllerBase
{
    [HttpGet]
    public async Task<DashboardResponse> Get(CancellationToken token) { var today = DateOnly.FromDateTime(DateTime.Today); var month = new DateOnly(today.Year, today.Month, 1); var access = await restrictedAccess.GetScopeAsync(User); return new(await context.Patients.CountAsync(token), await context.Appointments.CountAsync(x => x.AppointmentDate == today, token), await context.Appointments.CountAsync(x => x.AppointmentDate == today && x.Status == AppointmentStatus.Scheduled, token), await access.Filter(context.Episodes).CountAsync(x => x.Status == RecordStatus.Active, token), await access.Filter(context.Assessments).CountAsync(x => x.AssessedOn >= month, token), await access.Filter(context.Deliveries).CountAsync(x => x.DeliveryDate >= month, token)); }
}
