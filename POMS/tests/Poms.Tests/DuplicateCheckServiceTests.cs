using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;

namespace Poms.Tests;

public class DuplicateCheckServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PomsDbContext _context;
    private readonly int _provinceId;
    private readonly int _districtId;
    private readonly int _centerId;

    public DuplicateCheckServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PomsDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new PomsDbContext(options);
        _context.Database.EnsureCreated();

        var province = new Province { Code = "WP", Name = "Western Province" };
        _context.Provinces.Add(province);
        _context.SaveChanges();

        var district = new District { ProvinceId = province.Id, Code = "GMP", Name = "Gampaha" };
        _context.Districts.Add(district);
        _context.SaveChanges();

        var center = new Center { DistrictId = district.Id, Code = "RAG", Name = "Ragama" };
        _context.Centers.Add(center);
        _context.SaveChanges();

        _provinceId = province.Id;
        _districtId = district.Id;
        _centerId = center.Id;
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private Patient AddPatient(string patientNumber, IdentificationType idType, string idNumber, string fullName, DateOnly dob)
    {
        var patient = new Patient
        {
            PatientNumber = patientNumber,
            FullName = fullName,
            NameWithInitials = fullName,
            Dob = dob,
            Sex = Sex.Male,
            Address1 = "123 Main Street",
            ProvinceId = _provinceId,
            DistrictId = _districtId,
            Category = PatientCategory.Local,
            IdentificationType = idType,
            IdentificationNumber = idNumber,
            CenterId = _centerId,
            RegistrationDate = new DateOnly(2026, 1, 1),
            RegistrationProcessedBy = "test",
            GuardianName = "Guardian",
            GuardianRelationship = "Parent"
        };
        _context.Patients.Add(patient);
        _context.SaveChanges();
        return patient;
    }

    [Fact]
    public async Task CheckAsync_NoMatch_ReturnsNoDuplicate()
    {
        var service = new DuplicateCheckService(_context);

        var result = await service.CheckAsync(IdentificationType.NIC, "901234567V", "Kamal Perera", new DateOnly(1990, 5, 15));

        result.IsExactDuplicate.Should().BeFalse();
        result.HasSimilarNameOrDob.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_SameIdentificationTypeAndNumber_IsExactDuplicate()
    {
        AddPatient("2026/0001", IdentificationType.NIC, "901234567V", "Kamal Perera", new DateOnly(1990, 5, 15));
        var service = new DuplicateCheckService(_context);

        var result = await service.CheckAsync(IdentificationType.NIC, "901234567V", "Different Name", new DateOnly(1985, 1, 1));

        result.IsExactDuplicate.Should().BeTrue();
        result.ExistingPatientNumber.Should().Be("2026/0001");
        result.ExistingPatientName.Should().Be("Kamal Perera");
    }

    [Fact]
    public async Task CheckAsync_SameNumberDifferentIdType_IsNotExactDuplicate()
    {
        AddPatient("2026/0001", IdentificationType.NIC, "901234567V", "Kamal Perera", new DateOnly(1990, 5, 15));
        var service = new DuplicateCheckService(_context);

        var result = await service.CheckAsync(IdentificationType.Passport, "901234567V", "Kamal Perera", new DateOnly(1990, 5, 15));

        result.IsExactDuplicate.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAsync_SameNameAndDob_IsSimilarOnly()
    {
        AddPatient("2026/0001", IdentificationType.NIC, "901234567V", "Kamal Perera", new DateOnly(1990, 5, 15));
        var service = new DuplicateCheckService(_context);

        var result = await service.CheckAsync(IdentificationType.Passport, "N1234567", "Kamal Perera", new DateOnly(1990, 5, 15));

        result.IsExactDuplicate.Should().BeFalse();
        result.HasSimilarNameOrDob.Should().BeTrue();
        result.ExistingPatientNumber.Should().Be("2026/0001");
    }

    [Fact]
    public async Task CheckAsync_ExcludePatientId_IgnoresOwnRecordOnEdit()
    {
        var patient = AddPatient("2026/0001", IdentificationType.NIC, "901234567V", "Kamal Perera", new DateOnly(1990, 5, 15));
        var service = new DuplicateCheckService(_context);

        var result = await service.CheckAsync(IdentificationType.NIC, "901234567V", "Kamal Perera", new DateOnly(1990, 5, 15), excludePatientId: patient.Id);

        result.IsExactDuplicate.Should().BeFalse();
        result.HasSimilarNameOrDob.Should().BeFalse();
    }
}
