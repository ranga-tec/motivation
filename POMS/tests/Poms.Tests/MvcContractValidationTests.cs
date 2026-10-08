using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Poms.Domain.Enums;
using Poms.Web.Api.V1.Contracts;
using Poms.Web.Api.V1.Controllers;

namespace Poms.Tests;

public sealed class MvcContractValidationTests
{
    [Fact]
    public void PatientRegistrationContact_UsesConstructorParameterValidationMetadata()
    {
        using var provider = BuildMvcServices();
        var context = CreateActionContext(provider);
        var request = new CreatePatientRequest
        {
            Contacts = [new CreatePatientContactRequest("0771234567", null, "Reception")]
        };

        var exception = Record.Exception(() => provider
            .GetRequiredService<IObjectModelValidator>()
            .Validate(context, null, string.Empty, request));

        Assert.Null(exception);
    }

    [Fact]
    public void ClinicalPositionalRecords_UseConstructorParameterValidationMetadata()
    {
        using var provider = BuildMvcServices();
        var validator = provider.GetRequiredService<IObjectModelValidator>();
        object[] contracts =
        [
            new PrescriptionRequest(Side.Left, "OTHER", null, "Test"),
            new SaveFittingRequest(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), null, false),
            new SaveDeliveryRequest(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), null, null, null, false)
        ];

        var exception = Record.Exception(() =>
        {
            foreach (var contract in contracts)
                validator.Validate(CreateActionContext(provider), null, string.Empty, contract);
        });

        Assert.Null(exception);
    }

    private static ServiceProvider BuildMvcServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers().AddApplicationPart(typeof(PatientsApiController).Assembly);
        return services.BuildServiceProvider();
    }

    private static ActionContext CreateActionContext(IServiceProvider provider) => new(
        new DefaultHttpContext { RequestServices = provider },
        new RouteData(),
        new ActionDescriptor(),
        new ModelStateDictionary());
}
