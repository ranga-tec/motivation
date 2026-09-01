using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Poms.Domain.Enums;
using Poms.Web.ViewModels;

namespace Poms.Tests;

/// <summary>
/// Legacy and infant records frequently have no identification document and no
/// email address, so both fields have to accept "not on file" without blocking
/// registration.
/// </summary>
public class PatientOptionalIdentityTests
{
    private static PatientViewModel ValidPatient() => new()
    {
        FullName = "Test Patient",
        NameWithInitials = "T. Patient",
        Dob = new DateOnly(1990, 1, 1),
        Sex = Sex.Female,
        Category = PatientCategory.Local,
        IdentificationType = IdentificationType.NIC,
        IdentificationNumber = "199012345678",
        Address1 = "1 Test Road",
        ProvinceId = 1,
        DistrictId = 1,
        CityId = 1,
        CenterId = 1,
        GuardianName = "Test Guardian",
        GuardianRelationship = "Mother",
        RegistrationProcessedBy = "Tester",
        AssignedClinicianEntry = "Test Clinician"
    };

    private static IReadOnlyList<ValidationResult> Validate(PatientViewModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static bool HasErrorFor(IReadOnlyList<ValidationResult> results, string member) =>
        results.Any(result => result.MemberNames.Contains(member));

    [Fact]
    public void IdentificationType_OffersNotApplicable()
    {
        Enum.GetValues<IdentificationType>().Should().Contain(IdentificationType.NotApplicable);
    }

    [Fact]
    public void IdentificationNumber_IsRequiredForARealDocumentType()
    {
        var model = ValidPatient();
        model.IdentificationNumber = "";

        HasErrorFor(Validate(model), nameof(PatientViewModel.IdentificationNumber)).Should().BeTrue();
    }

    [Fact]
    public void IdentificationNumber_MayBeBlankWhenTypeIsNotApplicable()
    {
        var model = ValidPatient();
        model.IdentificationType = IdentificationType.NotApplicable;
        model.IdentificationNumber = "";

        HasErrorFor(Validate(model), nameof(PatientViewModel.IdentificationNumber)).Should().BeFalse();
    }

    [Theory]
    [InlineData("N/A")]
    [InlineData("n/a")]
    [InlineData("NA")]
    [InlineData("clinic@poms.lk")]
    [InlineData(null)]
    [InlineData("")]
    public void Email_AcceptsRealAddressesAndNotApplicable(string? email)
    {
        var model = ValidPatient();
        model.Email = email;

        HasErrorFor(Validate(model), nameof(PatientViewModel.Email)).Should().BeFalse();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    public void Email_StillRejectsMalformedAddresses(string email)
    {
        var model = ValidPatient();
        model.Email = email;

        HasErrorFor(Validate(model), nameof(PatientViewModel.Email)).Should().BeTrue();
    }

    [Theory]
    [InlineData("N/A", true)]
    [InlineData(" n/a ", true)]
    [InlineData("NA", true)]
    [InlineData("nathan@poms.lk", false)]
    [InlineData(null, false)]
    public void IsNotApplicable_RecognisesTheSpellingsStaffType(string? value, bool expected)
    {
        PatientViewModel.IsNotApplicable(value).Should().Be(expected);
    }
}
