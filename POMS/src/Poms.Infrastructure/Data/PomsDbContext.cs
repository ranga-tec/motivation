using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Poms.Domain.Entities;

namespace Poms.Infrastructure.Data;

public class PomsDbContext : IdentityDbContext
{
    public PomsDbContext(DbContextOptions<PomsDbContext> options) : base(options) { }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<PatientCondition> PatientConditions => Set<PatientCondition>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<EpisodeDocument> EpisodeDocuments => Set<EpisodeDocument>();
    public DbSet<ProstheticEpisode> ProstheticEpisodes => Set<ProstheticEpisode>();
    public DbSet<OrthoticEpisode> OrthoticEpisodes => Set<OrthoticEpisode>();
    public DbSet<SpinalEpisode> SpinalEpisodes => Set<SpinalEpisode>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Fitting> Fittings => Set<Fitting>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Repair> Repairs => Set<Repair>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<Center> Centers => Set<Center>();
    public DbSet<Condition> Conditions => Set<Condition>();
    public DbSet<DeviceType> DeviceTypes => Set<DeviceType>();
    public DbSet<DeviceCatalog> DeviceCatalogs => Set<DeviceCatalog>();
    public DbSet<ComponentCatalog> ComponentCatalogs => Set<ComponentCatalog>();
    public DbSet<NumberSeries> NumberSeries => Set<NumberSeries>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Patient Configuration
        builder.Entity<Patient>(entity =>
        {
            entity.HasIndex(e => e.PatientNumber).IsUnique();
            entity.Property(e => e.PatientNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.NationalId).HasMaxLength(50);
            entity.Property(e => e.Sex).HasConversion<string>();
            
            entity.HasOne(e => e.Province).WithMany().HasForeignKey(e => e.ProvinceId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.District).WithMany().HasForeignKey(e => e.DistrictId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Center).WithMany().HasForeignKey(e => e.CenterId).OnDelete(DeleteBehavior.Restrict);
            
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Episode Configuration
        builder.Entity<Episode>(entity =>
        {
            entity.Property(e => e.Type).HasConversion<string>();
            entity.HasOne(e => e.Patient).WithMany(p => p.Episodes).HasForeignKey(e => e.PatientId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Prosthetic Episode (one-to-one)
        builder.Entity<ProstheticEpisode>(entity =>
        {
            entity.HasKey(e => e.EpisodeId);
            entity.Property(e => e.AmputationType).HasConversion<string>();
            entity.Property(e => e.Side).HasConversion<string>();
            entity.Property(e => e.Reason).HasConversion<string>();
            entity.HasOne(e => e.Episode).WithOne(ep => ep.Prosthetic).HasForeignKey<ProstheticEpisode>(e => e.EpisodeId);
        });

        // Orthotic Episode (one-to-one)
        builder.Entity<OrthoticEpisode>(entity =>
        {
            entity.HasKey(e => e.EpisodeId);
            entity.Property(e => e.BodyRegion).HasConversion<string>();
            entity.Property(e => e.Side).HasConversion<string>();
            entity.HasOne(e => e.Episode).WithOne(ep => ep.Orthotic).HasForeignKey<OrthoticEpisode>(e => e.EpisodeId);
        });

        // Spinal Episode (one-to-one)
        builder.Entity<SpinalEpisode>(entity =>
        {
            entity.HasKey(e => e.EpisodeId);
            entity.HasOne(e => e.Episode).WithOne(ep => ep.Spinal).HasForeignKey<SpinalEpisode>(e => e.EpisodeId);
        });

        // Assessment
        builder.Entity<Assessment>(entity =>
        {
            entity.HasOne(e => e.Episode).WithMany(ep => ep.Assessments).HasForeignKey(e => e.EpisodeId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Fitting
        builder.Entity<Fitting>(entity =>
        {
            entity.HasOne(e => e.Episode).WithMany(ep => ep.Fittings).HasForeignKey(e => e.EpisodeId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Delivery (one-to-one with Episode)
        builder.Entity<Delivery>(entity =>
        {
            entity.HasOne(e => e.Episode).WithOne(ep => ep.Delivery).HasForeignKey<Delivery>(e => e.EpisodeId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // FollowUp
        builder.Entity<FollowUp>(entity =>
        {
            entity.HasOne(e => e.Episode).WithMany(ep => ep.FollowUps).HasForeignKey(e => e.EpisodeId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // Repair
        builder.Entity<Repair>(entity =>
        {
            entity.Property(e => e.Category).HasConversion<string>();
            entity.HasOne(e => e.Episode).WithMany(ep => ep.Repairs).HasForeignKey(e => e.EpisodeId);
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        // PatientCondition

        builder.Entity<PatientCondition>(entity =>
        {
            entity.Property(e => e.Side).HasConversion<string>();
            entity.Property(e => e.Type).HasConversion<string>();
            entity.HasOne(e => e.Patient).WithMany(p => p.Conditions).HasForeignKey(e => e.PatientId);
            entity.HasOne(e => e.Condition).WithMany().HasForeignKey(e => e.ConditionId);
        });

        // Condition
        builder.Entity<Condition>(entity =>
        {
            entity.Property(e => e.BodyRegion).HasConversion<string>();
            entity.HasIndex(e => e.Code).IsUnique();
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

        builder.Entity<Center>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.HasOne(e => e.District).WithMany(d => d.Centers).HasForeignKey(e => e.DistrictId);
        });

        // NumberSeries - Composite unique index
        builder.Entity<NumberSeries>(entity =>
        {
            entity.HasIndex(e => new { e.CenterId, e.Year }).IsUnique();
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

        // Sample Provinces (Sri Lanka)
        builder.Entity<Province>().HasData(
            new Province { Id = 1, Code = "WP", Name = "Western Province" },
            new Province { Id = 2, Code = "CP", Name = "Central Province" },
            new Province { Id = 3, Code = "SP", Name = "Southern Province" },
            new Province { Id = 4, Code = "NP", Name = "Northern Province" },
            new Province { Id = 5, Code = "EP", Name = "Eastern Province" }
        );

        // Sample Districts
        builder.Entity<District>().HasData(
            new District { Id = 1, ProvinceId = 1, Code = "CO", Name = "Colombo" },
            new District { Id = 2, ProvinceId = 1, Code = "GM", Name = "Gampaha" },
            new District { Id = 3, ProvinceId = 2, Code = "KA", Name = "Kandy" },
            new District { Id = 4, ProvinceId = 3, Code = "GL", Name = "Galle" },
            new District { Id = 5, ProvinceId = 4, Code = "JA", Name = "Jaffna" }
        );

        // Sample Centers
        builder.Entity<Center>().HasData(
            new Center { Id = 1, DistrictId = 1, Code = "RAG", Name = "Ragama Rehabilitation Center", Address = "Ragama", Phone = "011-1234567" },
            new Center { Id = 2, DistrictId = 3, Code = "KDY", Name = "Kandy P&O Center", Address = "Kandy", Phone = "081-1234567" },
            new Center { Id = 3, DistrictId = 4, Code = "GAL", Name = "Galle Orthotic Center", Address = "Galle", Phone = "091-1234567" }
        );
    }
}
