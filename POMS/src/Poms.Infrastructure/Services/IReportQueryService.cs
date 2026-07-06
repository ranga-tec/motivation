using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;
using Poms.Domain.Enums;
using Poms.Infrastructure.Data;

namespace Poms.Infrastructure.Services;

public class ReportFilter
{
    public int? CenterId { get; set; }
    public int? ProvinceId { get; set; }
    public DateOnly? DateFrom { get; set; } = new DateOnly(DateTime.Today.Year, 1, 1);
    public DateOnly? DateTo { get; set; }
    public Guid? PatientId { get; set; }
    public string? PatientNumber { get; set; }
    public bool? IsForeign { get; set; }
    public AssessmentType? AssessmentType { get; set; }
    public LimbCategory? LimbCategory { get; set; }
    public string? PrescriptionCode { get; set; }
    public DateOnly? FittingDateFrom { get; set; }
    public DateOnly? FittingDateTo { get; set; }
    public DateOnly? DeliveryDateFrom { get; set; }
    public DateOnly? DeliveryDateTo { get; set; }
    public DateOnly? FollowUpDateFrom { get; set; }
    public DateOnly? FollowUpDateTo { get; set; }
    public RecordStatus? RecordStatus { get; set; }
    public int? Year { get; set; }
}

public interface IReportQueryService
{
    IQueryable<Patient> FilterPatients(ReportFilter filter);
    IQueryable<Episode> FilterEpisodes(ReportFilter filter);
    IQueryable<Assessment> FilterAssessments(ReportFilter filter);
    IQueryable<Fitting> FilterFittings(ReportFilter filter);
    IQueryable<Delivery> FilterDeliveries(ReportFilter filter);
    IQueryable<FollowUp> FilterFollowUps(ReportFilter filter);
}

public class ReportQueryService : IReportQueryService
{
    private readonly PomsDbContext _context;

    public ReportQueryService(PomsDbContext context)
    {
        _context = context;
    }

    public IQueryable<Patient> FilterPatients(ReportFilter filter)
    {
        var query = _context.Patients
            .Include(p => p.Province).Include(p => p.District).Include(p => p.Center)
            .AsQueryable();

        if (filter.CenterId.HasValue) query = query.Where(p => p.CenterId == filter.CenterId);
        if (filter.ProvinceId.HasValue) query = query.Where(p => p.ProvinceId == filter.ProvinceId);
        if (filter.DateFrom.HasValue) query = query.Where(p => p.RegistrationDate >= filter.DateFrom);
        if (filter.DateTo.HasValue) query = query.Where(p => p.RegistrationDate <= filter.DateTo);
        if (filter.PatientId.HasValue) query = query.Where(p => p.Id == filter.PatientId);
        if (!string.IsNullOrWhiteSpace(filter.PatientNumber)) query = query.Where(p => p.PatientNumber.Contains(filter.PatientNumber));
        if (filter.IsForeign.HasValue)
            query = query.Where(p => p.Category == (filter.IsForeign.Value ? PatientCategory.Foreign : PatientCategory.Local));
        if (filter.Year.HasValue) query = query.Where(p => p.RegistrationDate.Year == filter.Year);

        return query;
    }

    public IQueryable<Episode> FilterEpisodes(ReportFilter filter)
    {
        var query = _context.Episodes
            .Include(e => e.Patient).Include(e => e.Center)
            .AsQueryable();

        if (filter.CenterId.HasValue) query = query.Where(e => e.CenterId == filter.CenterId);
        if (filter.ProvinceId.HasValue) query = query.Where(e => e.Patient.ProvinceId == filter.ProvinceId);
        if (filter.DateFrom.HasValue) query = query.Where(e => e.RecordDate >= filter.DateFrom);
        if (filter.DateTo.HasValue) query = query.Where(e => e.RecordDate <= filter.DateTo);
        if (filter.PatientId.HasValue) query = query.Where(e => e.PatientId == filter.PatientId);
        if (!string.IsNullOrWhiteSpace(filter.PatientNumber)) query = query.Where(e => e.Patient.PatientNumber.Contains(filter.PatientNumber));
        if (filter.IsForeign.HasValue)
            query = query.Where(e => e.Patient.Category == (filter.IsForeign.Value ? PatientCategory.Foreign : PatientCategory.Local));
        if (filter.RecordStatus.HasValue) query = query.Where(e => e.Status == filter.RecordStatus);
        if (filter.Year.HasValue) query = query.Where(e => e.RecordDate.Year == filter.Year);

        return query;
    }

    public IQueryable<Assessment> FilterAssessments(ReportFilter filter)
    {
        var query = _context.Assessments
            .Include(a => a.Episode).ThenInclude(e => e.Patient)
            .Include(a => a.Episode).ThenInclude(e => e.Center)
            .Include(a => a.MainProblemType)
            .Include(a => a.CauseReasonType)
            .Include(a => a.Prescriptions)
            .AsQueryable();

        if (filter.CenterId.HasValue) query = query.Where(a => a.Episode.CenterId == filter.CenterId);
        if (filter.ProvinceId.HasValue) query = query.Where(a => a.Episode.Patient.ProvinceId == filter.ProvinceId);
        if (filter.DateFrom.HasValue) query = query.Where(a => a.AssessedOn >= filter.DateFrom);
        if (filter.DateTo.HasValue) query = query.Where(a => a.AssessedOn <= filter.DateTo);
        if (filter.PatientId.HasValue) query = query.Where(a => a.Episode.PatientId == filter.PatientId);
        if (!string.IsNullOrWhiteSpace(filter.PatientNumber)) query = query.Where(a => a.Episode.Patient.PatientNumber.Contains(filter.PatientNumber));
        if (filter.AssessmentType.HasValue) query = query.Where(a => a.AssessmentType == filter.AssessmentType);
        if (filter.LimbCategory.HasValue) query = query.Where(a => a.LimbCategory == filter.LimbCategory);
        if (!string.IsNullOrWhiteSpace(filter.PrescriptionCode)) query = query.Where(a => a.Prescriptions.Any(p => p.PrescriptionCode == filter.PrescriptionCode));
        if (filter.Year.HasValue) query = query.Where(a => a.AssessedOn.Year == filter.Year);

        return query;
    }

    public IQueryable<Fitting> FilterFittings(ReportFilter filter)
    {
        var query = _context.Fittings
            .Include(f => f.Episode).ThenInclude(e => e.Patient)
            .Include(f => f.Episode).ThenInclude(e => e.Center)
            .AsQueryable();

        if (filter.CenterId.HasValue) query = query.Where(f => f.Episode.CenterId == filter.CenterId);
        if ((filter.FittingDateFrom ?? filter.DateFrom).HasValue) query = query.Where(f => f.FittingDate >= (filter.FittingDateFrom ?? filter.DateFrom));
        if ((filter.FittingDateTo ?? filter.DateTo).HasValue) query = query.Where(f => f.FittingDate <= (filter.FittingDateTo ?? filter.DateTo));
        if (filter.PatientId.HasValue) query = query.Where(f => f.Episode.PatientId == filter.PatientId);
        if (!string.IsNullOrWhiteSpace(filter.PatientNumber)) query = query.Where(f => f.Episode.Patient.PatientNumber.Contains(filter.PatientNumber));

        return query;
    }

    public IQueryable<Delivery> FilterDeliveries(ReportFilter filter)
    {
        var query = _context.Deliveries
            .Include(d => d.Episode).ThenInclude(e => e.Patient)
            .Include(d => d.Episode).ThenInclude(e => e.Center)
            .AsQueryable();

        if (filter.CenterId.HasValue) query = query.Where(d => d.Episode.CenterId == filter.CenterId);
        if ((filter.DeliveryDateFrom ?? filter.DateFrom).HasValue) query = query.Where(d => d.DeliveryDate >= (filter.DeliveryDateFrom ?? filter.DateFrom));
        if ((filter.DeliveryDateTo ?? filter.DateTo).HasValue) query = query.Where(d => d.DeliveryDate <= (filter.DeliveryDateTo ?? filter.DateTo));
        if (filter.PatientId.HasValue) query = query.Where(d => d.Episode.PatientId == filter.PatientId);
        if (!string.IsNullOrWhiteSpace(filter.PatientNumber)) query = query.Where(d => d.Episode.Patient.PatientNumber.Contains(filter.PatientNumber));

        return query;
    }

    public IQueryable<FollowUp> FilterFollowUps(ReportFilter filter)
    {
        var query = _context.FollowUps
            .Include(f => f.Episode).ThenInclude(e => e.Patient)
            .Include(f => f.Episode).ThenInclude(e => e.Center)
            .AsQueryable();

        if (filter.CenterId.HasValue) query = query.Where(f => f.Episode.CenterId == filter.CenterId);
        if ((filter.FollowUpDateFrom ?? filter.DateFrom).HasValue) query = query.Where(f => f.FollowUpDate >= (filter.FollowUpDateFrom ?? filter.DateFrom));
        if ((filter.FollowUpDateTo ?? filter.DateTo).HasValue) query = query.Where(f => f.FollowUpDate <= (filter.FollowUpDateTo ?? filter.DateTo));
        if (filter.PatientId.HasValue) query = query.Where(f => f.Episode.PatientId == filter.PatientId);
        if (!string.IsNullOrWhiteSpace(filter.PatientNumber)) query = query.Where(f => f.Episode.Patient.PatientNumber.Contains(filter.PatientNumber));

        return query;
    }
}
