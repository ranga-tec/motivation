using Microsoft.EntityFrameworkCore;
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

        // Use transaction to ensure concurrency safety
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var numberSeries = await _context.NumberSeries
                .FirstOrDefaultAsync(x => x.CenterId == centerId && x.Year == year);

            if (numberSeries == null)
            {
                numberSeries = new Domain.Entities.NumberSeries 
                { 
                    CenterId = centerId, 
                    Year = year, 
                    LastSeq = 0 
                };
                _context.NumberSeries.Add(numberSeries);
            }

            numberSeries.LastSeq += 1;
            await _context.SaveChangesAsync();

            var center = await _context.Centers
                .Include(c => c.District)
                .ThenInclude(d => d.Province)
                .FirstOrDefaultAsync(c => c.Id == centerId);

            if (center == null)
                throw new InvalidOperationException($"Center with ID {centerId} not found");

            var patientNumber = $"{center.District.Province.Code}{center.District.Code}-{center.Code}-{year}-{numberSeries.LastSeq:D4}";

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
