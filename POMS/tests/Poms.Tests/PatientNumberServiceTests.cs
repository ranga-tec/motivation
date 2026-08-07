using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;
using Poms.Infrastructure.Services;

namespace Poms.Tests;

public class PatientNumberServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PomsDbContext _context;
    private readonly int _unflaggedCenterId;
    private readonly int _flaggedCenterId;

    public PatientNumberServiceTests()
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

        var unflagged = new Center { DistrictId = district.Id, Code = "RAG", Name = "Ragama", RequiresPatientNumberFlag = false };
        var flagged = new Center { DistrictId = district.Id, Code = "COL", Name = "Colombo", RequiresPatientNumberFlag = true, PatientNumberFlagCode = "C" };
        _context.Centers.AddRange(unflagged, flagged);
        _context.SaveChanges();

        _unflaggedCenterId = unflagged.Id;
        _flaggedCenterId = flagged.Id;
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GeneratePatientNumberAsync_UnflaggedCenter_ProducesYearSlashSeq()
    {
        var service = new PatientNumberService(_context);

        var result = await service.GeneratePatientNumberAsync(_unflaggedCenterId, new DateOnly(2026, 6, 21));

        result.Should().Be("2026/0001");
        (await _context.NumberSeries.SingleAsync()).CenterId.Should().Be(_unflaggedCenterId);
    }

    [Fact]
    public async Task GeneratePatientNumberAsync_FlaggedCenter_PrefixesFlagCode()
    {
        var service = new PatientNumberService(_context);

        var result = await service.GeneratePatientNumberAsync(_flaggedCenterId, new DateOnly(2026, 6, 21));

        result.Should().Be("C/2026/0001");
    }

    [Fact]
    public async Task GeneratePatientNumberAsync_SequentialCallsSameYear_Increment()
    {
        var service = new PatientNumberService(_context);

        var first = await service.GeneratePatientNumberAsync(_unflaggedCenterId, new DateOnly(2026, 6, 21));
        var second = await service.GeneratePatientNumberAsync(_unflaggedCenterId, new DateOnly(2026, 7, 1));

        first.Should().Be("2026/0001");
        second.Should().Be("2026/0002");
    }

    [Fact]
    public async Task GeneratePatientNumberAsync_FlaggedAndUnflagged_AreIndependentSequences()
    {
        var service = new PatientNumberService(_context);

        var unflagged = await service.GeneratePatientNumberAsync(_unflaggedCenterId, new DateOnly(2026, 6, 21));
        var flagged = await service.GeneratePatientNumberAsync(_flaggedCenterId, new DateOnly(2026, 6, 21));

        unflagged.Should().Be("2026/0001");
        flagged.Should().Be("C/2026/0001");
    }

    [Fact]
    public async Task GeneratePatientNumberAsync_DifferentYears_ResetSequence()
    {
        var service = new PatientNumberService(_context);

        var year2026 = await service.GeneratePatientNumberAsync(_unflaggedCenterId, new DateOnly(2026, 12, 31));
        var year2027 = await service.GeneratePatientNumberAsync(_unflaggedCenterId, new DateOnly(2027, 1, 1));

        year2026.Should().Be("2026/0001");
        year2027.Should().Be("2027/0001");
    }

    [Fact]
    public async Task GeneratePatientNumberAsync_UnknownCenter_Throws()
    {
        var service = new PatientNumberService(_context);

        var act = async () => await service.GeneratePatientNumberAsync(9999, new DateOnly(2026, 6, 21));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
