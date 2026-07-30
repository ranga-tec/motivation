using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;

namespace Poms.Infrastructure.Data;

public class PomsDbContext : IdentityDbContext
{
    public PomsDbContext(DbContextOptions<PomsDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientContact> PatientContacts => Set<PatientContact>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<EpisodeDocument> EpisodeDocuments => Set<EpisodeDocument>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<Fitting> Fittings => Set<Fitting>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Center> Centers => Set<Center>();
    public DbSet<ReferralSource> ReferralSources => Set<ReferralSource>();
    public DbSet<MainProblemType> MainProblemTypes => Set<MainProblemType>();
    public DbSet<CauseReasonType> CauseReasonTypes => Set<CauseReasonType>();
    public DbSet<Nationality> Nationalities => Set<Nationality>();
    public DbSet<DeviceType> DeviceTypes => Set<DeviceType>();
    public DbSet<DeviceCatalog> DeviceCatalogs => Set<DeviceCatalog>();
    public DbSet<ComponentCatalog> ComponentCatalogs => Set<ComponentCatalog>();
    public DbSet<NumberSeries> NumberSeries => Set<NumberSeries>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Patient Configuration
        builder.Entity<Patient>(entity =>
        {
            entity.HasIndex(e => e.PatientNumber).IsUnique();
            entity.Property(e => e.PatientNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NameWithInitials).HasMaxLength(100).IsRequired();
            entity.Property(e => e.IdentificationNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Sex).HasConversion<string>();
            entity.Property(e => e.Category).HasConversion<string>();
            entity.Property(e => e.IdentificationType).HasConversion<string>();
            entity.HasIndex(e => new { e.IdentificationType, e.IdentificationNumber });

            entity.HasOne(e => e.Province).WithMany().HasForeignKey(e => e.ProvinceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.District).WithMany().HasForeignKey(e => e.DistrictId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.City).WithMany().HasForeignKey(e => e.CityId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Center).WithMany().HasForeignKey(e => e.CenterId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ReferralSource).WithMany().HasForeignKey(e => e.ReferralSourceId).OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<PatientContact>(entity =>
        {
            entity.HasOne(e => e.Patient).WithMany(p => p.Contacts).HasForeignKey(e => e.PatientId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Episode Configuration
        builder.Entity<Episode>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CenterId);
            entity.HasIndex(e => e.IsRestricted);
            entity.HasOne(e => e.Patient).WithMany(p => p.Episodes).HasForeignKey(e => e.PatientId);
            entity.HasOne(e => e.Center).WithMany().HasForeignKey(e => e.CenterId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Assessment
        builder.Entity<Assessment>(entity =>
        {
            entity.Property(e => e.AssessmentType).HasConversion<string>();
            entity.Property(e => e.LimbCategory).HasConversion<string>();
            entity.Property(e => e.Side).HasConversion<string>();
            entity.HasIndex(e => e.AssessedOn);
            entity.HasIndex(e => e.IsRestricted);
            entity.HasOne(e => e.Episode).WithMany(ep => ep.Assessments).HasForeignKey(e => e.EpisodeId);
            entity.HasOne(e => e.MainProblemType).WithMany().HasForeignKey(e => e.MainProblemTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CauseReasonType).WithMany().HasForeignKey(e => e.CauseReasonTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Prescription
        builder.Entity<Prescription>(entity =>
        {
            entity.Property(e => e.Side).HasConversion<string>();
            entity.HasIndex(e => new { e.AssessmentId, e.Side }).IsUnique();
            entity.HasOne(e => e.Assessment).WithMany(a => a.Prescriptions).HasForeignKey(e => e.AssessmentId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Fitting
        builder.Entity<Fitting>(entity =>
        {
            entity.HasIndex(e => e.FittingDate);
            entity.HasIndex(e => e.IsRestricted);
            entity.HasOne(e => e.Episode).WithMany(ep => ep.Fittings).HasForeignKey(e => e.EpisodeId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Delivery (one-to-many with Episode)
        builder.Entity<Delivery>(entity =>
        {
            entity.HasIndex(e => e.DeliveryDate);
            entity.HasIndex(e => e.IsRestricted);
            entity.HasOne(e => e.Episode).WithMany(ep => ep.Deliveries).HasForeignKey(e => e.EpisodeId);
            entity.HasOne(e => e.Device).WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // FollowUp
        builder.Entity<FollowUp>(entity =>
        {
            entity.HasIndex(e => e.FollowUpDate);
            entity.HasIndex(e => e.IsRestricted);
            entity.HasOne(e => e.Episode).WithMany(ep => ep.FollowUps).HasForeignKey(e => e.EpisodeId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Appointment
        builder.Entity<Appointment>(entity =>
        {
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.AssignedClinicianUserId).HasMaxLength(450);
            entity.Property(e => e.AssignedClinicianName).HasMaxLength(200);
            entity.Property(e => e.CancellationReason)
                .HasMaxLength(Appointment.CancellationReasonMaxLength);
            entity.HasIndex(e => e.AppointmentDate);
            entity.HasIndex(e => e.AssignedClinicianUserId);
            entity.HasOne(e => e.Patient).WithMany(p => p.Appointments).HasForeignKey(e => e.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Episode).WithMany(ep => ep.Appointments).HasForeignKey(e => e.EpisodeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<IdentityUser>()
                .WithMany()
                .HasForeignKey(e => e.AssignedClinicianUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // PatientDocument / EpisodeDocument
        builder.Entity<PatientDocument>(entity =>
        {
            entity.Property(e => e.DocumentType).HasConversion<string>();
            entity.HasIndex(e => e.IsRestricted);
            entity.HasOne(e => e.Patient).WithMany(p => p.Documents).HasForeignKey(e => e.PatientId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<EpisodeDocument>(entity =>
        {
            entity.Property(e => e.DocumentType).HasConversion<string>();
            entity.HasIndex(e => e.IsRestricted);
            entity.HasOne(e => e.Episode).WithMany(ep => ep.Documents).HasForeignKey(e => e.EpisodeId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        builder.Entity<EmployeeProfile>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.EmployeeNumber).IsUnique();
            entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
            entity.Property(e => e.EmployeeNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Designation).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Department).HasMaxLength(150);
            entity.Property(e => e.MobileNumber).HasMaxLength(30).IsRequired();
            entity.Property(e => e.WorkPhoneNumber).HasMaxLength(30);
            entity.HasOne<IdentityUser>()
                .WithOne()
                .HasForeignKey<EmployeeProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Location entities
        builder.Entity<Province>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        builder.Entity<District>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.HasOne(e => e.Province).WithMany(p => p.Districts).HasForeignKey(e => e.ProvinceId);
        });

        builder.Entity<City>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.HasIndex(e => new { e.DistrictId, e.Name });
            entity.HasOne(e => e.District).WithMany().HasForeignKey(e => e.DistrictId);
        });

        builder.Entity<Center>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.HasOne(e => e.District).WithMany(d => d.Centers).HasForeignKey(e => e.DistrictId);
        });

        // Master data lookup tables
        builder.Entity<ReferralSource>(entity => entity.HasIndex(e => e.Name).IsUnique());
        builder.Entity<MainProblemType>(entity => entity.HasIndex(e => e.Name).IsUnique());
        builder.Entity<CauseReasonType>(entity => entity.HasIndex(e => e.Name).IsUnique());
        builder.Entity<Nationality>(entity => entity.HasIndex(e => e.Name).IsUnique());

        // NumberSeries - composite unique index, rekeyed by flag code + year
        builder.Entity<NumberSeries>(entity =>
        {
            entity.HasIndex(e => new { e.FlagCode, e.Year }).IsUnique();
        });

        // Device Catalog
        builder.Entity<DeviceCatalog>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasOne(e => e.DeviceType).WithMany().HasForeignKey(e => e.DeviceTypeId);
        });

        builder.Entity<ComponentCatalog>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasOne(e => e.DeviceType).WithMany().HasForeignKey(e => e.DeviceTypeId);
        });

        // Seed initial data
        SeedData(builder);
    }

    private void SeedData(ModelBuilder builder)
    {
        // Device Types
        builder.Entity<DeviceType>().HasData(
            new DeviceType { Id = 1, Code = "PROS", Name = "Prosthesis" },
            new DeviceType { Id = 2, Code = "ORTH", Name = "Orthosis" },
            new DeviceType { Id = 3, Code = "SPIN", Name = "Spinal Orthosis" }
        );
    }
}
