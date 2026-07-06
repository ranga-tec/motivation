using Microsoft.EntityFrameworkCore;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;

namespace Poms.Infrastructure.Services;

public interface IDuplicateCheckService
{
    Task<DuplicateCheckResult> CheckAsync(IdentificationType idType, string idNumber,
        string? fullName, DateOnly? dob, Guid? excludePatientId = null);
}

public class DuplicateCheckResult
{
    public bool IsExactDuplicate { get; set; }
    public bool HasSimilarNameOrDob { get; set; }
    public string? ExistingPatientNumber { get; set; }
    public string? ExistingPatientName { get; set; }
}

public class DuplicateCheckService : IDuplicateCheckService
{
    private readonly PomsDbContext _context;

    public DuplicateCheckService(PomsDbContext context)
    {
        _context = context;
    }

    public async Task<DuplicateCheckResult> CheckAsync(IdentificationType idType, string idNumber,
        string? fullName, DateOnly? dob, Guid? excludePatientId = null)
    {
        var exact = await _context.Patients
            .Where(p => p.IdentificationType == idType && p.IdentificationNumber == idNumber)
            .Where(p => excludePatientId == null || p.Id != excludePatientId)
            .Select(p => new { p.PatientNumber, p.FullName })
            .FirstOrDefaultAsync();

        if (exact != null)
        {
            return new DuplicateCheckResult
            {
                IsExactDuplicate = true,
                ExistingPatientNumber = exact.PatientNumber,
                ExistingPatientName = exact.FullName
            };
        }

        var similar = !string.IsNullOrWhiteSpace(fullName) && dob.HasValue
            ? await _context.Patients
                .Where(p => p.FullName == fullName && p.Dob == dob.Value)
                .Where(p => excludePatientId == null || p.Id != excludePatientId)
                .Select(p => new { p.PatientNumber, p.FullName })
                .FirstOrDefaultAsync()
            : null;

        return new DuplicateCheckResult
        {
            HasSimilarNameOrDob = similar != null,
            ExistingPatientNumber = similar?.PatientNumber,
            ExistingPatientName = similar?.FullName
        };
    }
}
