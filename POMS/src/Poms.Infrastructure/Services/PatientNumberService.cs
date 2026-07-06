using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Infrastructure.Data;

namespace Poms.Infrastructure.Services;

public interface IPatientNumberService
{
    Task<string> GeneratePatientNumberAsync(int centerId, DateOnly registrationDate);
}

public class PatientNumberService : IPatientNumberService
{
    private readonly PomsDbContext _context;

    public PatientNumberService(PomsDbContext context)
    {
        _context = context;
    }

    public async Task<string> GeneratePatientNumberAsync(int centerId, DateOnly registrationDate)
    {
        var year = registrationDate.Year;
        var center = await _context.Centers.FirstOrDefaultAsync(c => c.Id == centerId)
            ?? throw new InvalidOperationException($"Center with ID {centerId} not found");

        var flagCode = center.RequiresPatientNumberFlag ? (center.PatientNumberFlagCode ?? "") : "";

        // Use transaction to ensure concurrency safety
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var series = await _context.NumberSeries
                .FirstOrDefaultAsync(x => x.FlagCode == flagCode && x.Year == year);

            if (series == null)
            {
                series = new NumberSeries { FlagCode = flagCode, Year = year, LastSeq = 0 };
                _context.NumberSeries.Add(series);
            }

            series.LastSeq += 1;
            await _context.SaveChangesAsync();

            var patientNumber = string.IsNullOrEmpty(flagCode)
                ? $"{year}/{series.LastSeq:D4}"
                : $"{flagCode}/{year}/{series.LastSeq:D4}";

            // Defensive uniqueness re-check before commit.
            if (await _context.Patients.AnyAsync(p => p.PatientNumber == patientNumber))
                throw new InvalidOperationException($"Generated duplicate patient number {patientNumber}, retry.");

            await transaction.CommitAsync();

            return patientNumber;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
