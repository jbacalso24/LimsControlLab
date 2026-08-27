using LimsControlLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LimsControlLab.Infrastructure;

// ponytail: Postgres has no rowversion type, so RowVersion is mapped as a
// plain bytea concurrency token (see OnModelCreating). EF still puts the
// ORIGINAL loaded value in the UPDATE WHERE clause for optimistic
// concurrency; these overrides just stamp a fresh value into the SET before
// every insert/update, mimicking what SQL Server auto-generated.

public sealed class LimsDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Sample> Samples { get; set; } = null!;
    public DbSet<Analysis> Analyses { get; set; } = null!;
    public DbSet<AnalysisTemplate> AnalysisTemplates { get; set; } = null!;
    public DbSet<AnalysisTemplateVersion> AnalysisTemplateVersions { get; set; } = null!;
    public DbSet<Reading> Readings { get; set; } = null!;
    public DbSet<ExceptionRecord> ExceptionRecords { get; set; } = null!;
    public DbSet<AuditLogEntry> AuditLogs { get; set; } = null!;
    public DbSet<Schedule> Schedules { get; set; } = null!;
    public DbSet<SamplingMethod> SamplingMethods { get; set; } = null!;
    public DbSet<Instrument> Instruments { get; set; } = null!;
    public DbSet<CalibrationCurve> CalibrationCurves { get; set; } = null!;
    public DbSet<CalibrationPoint> CalibrationPoints { get; set; } = null!;
    public DbSet<SampleTransfer> SampleTransfers { get; set; } = null!;
    public DbSet<IntegrationLogEntry> IntegrationLogs { get; set; } = null!;

    public LimsDbContext(DbContextOptions<LimsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.Site).IsRequired();
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasIndex(e => e.Username).IsUnique();
        });

        modelBuilder.Entity<AnalysisTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Site).IsRequired();
            entity.Property(e => e.CurrentVersionId);
            entity.Property(e => e.IsRetired).IsRequired();
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasOne(e => e.CurrentVersion)
                .WithMany()
                .HasForeignKey(e => e.CurrentVersionId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(e => e.Versions)
                .WithOne(v => v.Template)
                .HasForeignKey(v => v.TemplateId);
            entity.HasIndex(e => new { e.Site, e.Name }).IsUnique();
        });

        modelBuilder.Entity<AnalysisTemplateVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateId).IsRequired();
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.TestConfiguration);
            entity.Property(e => e.CalculationDefinitions);
            entity.Property(e => e.ValidationRules);
            entity.Property(e => e.MinTolerance);
            entity.Property(e => e.MaxTolerance);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasIndex(e => new { e.TemplateId, e.Version }).IsUnique();
        });

        modelBuilder.Entity<Sample>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Identifier).IsRequired().HasMaxLength(256);
            entity.Property(e => e.AnalysisTemplateId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Site).IsRequired();
            entity.Property(e => e.CurrentSite).IsRequired();
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasOne(e => e.AnalysisTemplate)
                .WithMany()
                .HasForeignKey(e => e.AnalysisTemplateId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(e => e.Transfers)
                .WithOne(t => t.Sample)
                .HasForeignKey(t => t.SampleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.Site, e.Identifier }).IsUnique();
        });

        modelBuilder.Entity<Analysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SampleId).IsRequired();
            entity.Property(e => e.TemplateId).IsRequired();
            entity.Property(e => e.TemplateVersionId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.StartedAtUtc).IsRequired();
            entity.Property(e => e.CompletedAtUtc);
            entity.Property(e => e.StartedByUserId).IsRequired();
            entity.Property(e => e.IsLocked).IsRequired();
            entity.Property(e => e.LockedAtUtc);
            entity.Property(e => e.LockedByUserId);
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasOne(e => e.Sample)
                .WithMany()
                .HasForeignKey(e => e.SampleId);
            entity.HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.TemplateId);
            entity.HasOne(e => e.TemplateVersion)
                .WithMany()
                .HasForeignKey(e => e.TemplateVersionId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(e => e.StartedAtUtc);
        });

        modelBuilder.Entity<Reading>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AnalysisId).IsRequired();
            entity.Property(e => e.TestId).IsRequired();
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.Unit).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CapturedAtUtc).IsRequired();
            entity.Property(e => e.CapturedByUserId).IsRequired();
            entity.Property(e => e.InstrumentId);
            entity.Property(e => e.ValidationResult).IsRequired().HasMaxLength(256);
            entity.Property(e => e.CalibratedValue);
            entity.HasOne(e => e.Analysis)
                .WithMany(a => a.Readings)
                .HasForeignKey(e => e.AnalysisId);
            entity.HasOne<Instrument>()
                .WithMany()
                .HasForeignKey(e => e.InstrumentId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(e => e.TestId);
            entity.HasIndex(e => e.InstrumentId);
        });

        modelBuilder.Entity<ExceptionRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AnalysisId).IsRequired();
            entity.Property(e => e.ReadingId).IsRequired();
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(512);
            entity.Property(e => e.Decision);
            entity.Property(e => e.DecisionComment);
            entity.Property(e => e.DecidedByUserId);
            entity.Property(e => e.DecidedAtUtc);
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasOne(e => e.Analysis)
                .WithMany(a => a.Exceptions)
                .HasForeignKey(e => e.AnalysisId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Reading)
                .WithMany()
                .HasForeignKey(e => e.ReadingId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TimestampUtc).IsRequired();
            entity.Property(e => e.Action).IsRequired().HasMaxLength(256);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(256);
            entity.Property(e => e.EntityId).IsRequired();
            entity.Property(e => e.BeforeValues);
            entity.Property(e => e.AfterValues);
            entity.Property(e => e.CorrelationId);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.TimestampUtc);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Site).IsRequired();
            entity.Property(e => e.AnalysisType).HasMaxLength(256);
            entity.Property(e => e.ShiftPattern).IsRequired();
            entity.Property(e => e.RecurrencePattern);
            entity.Property(e => e.ExclusionRules);
            entity.Property(e => e.AssignedToUserId);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasOne(e => e.AssignedToUser)
                .WithMany()
                .HasForeignKey(e => e.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(e => new { e.Site, e.Name }).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<SamplingMethod>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(512);
            entity.Property(e => e.Site).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasIndex(e => new { e.Site, e.Name }).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<Instrument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Model).HasMaxLength(256);
            entity.Property(e => e.SerialNumber).HasMaxLength(256);
            entity.Property(e => e.Site).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasIndex(e => new { e.Site, e.Name }).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<CalibrationCurve>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.AnalysisTemplateId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasOne(e => e.AnalysisTemplate)
                .WithMany()
                .HasForeignKey(e => e.AnalysisTemplateId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasMany(e => e.Points)
                .WithOne(p => p.CalibrationCurve)
                .HasForeignKey(p => p.CalibrationCurveId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.AnalysisTemplateId, e.Name }).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<CalibrationPoint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CalibrationCurveId).IsRequired();
            entity.Property(e => e.XValue).IsRequired();
            entity.Property(e => e.YValue).IsRequired();
            entity.Property(e => e.Order).IsRequired();
            entity.HasIndex(e => new { e.CalibrationCurveId, e.Order }).IsUnique();
        });

        modelBuilder.Entity<SampleTransfer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SampleId).IsRequired();
            entity.Property(e => e.FromSite).IsRequired();
            entity.Property(e => e.ToSite).IsRequired();
            entity.Property(e => e.TransferredByUserId).IsRequired();
            entity.Property(e => e.TransferredAtUtc).IsRequired();
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasOne(e => e.Sample)
                .WithMany(s => s.Transfers)
                .HasForeignKey(e => e.SampleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.SampleId);
            entity.HasIndex(e => e.TransferredAtUtc);
        });

        modelBuilder.Entity<IntegrationLogEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TargetSystem).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AnalysisId).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AttemptedAtUtc).IsRequired();
            entity.Property(e => e.CompletedAtUtc);
            entity.Property(e => e.ErrorMessage).HasMaxLength(512);
            entity.Property(e => e.RetryCount).IsRequired();
            entity.HasOne(e => e.Analysis)
                .WithMany()
                .HasForeignKey(e => e.AnalysisId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(e => new { e.TargetSystem, e.Status });
            entity.HasIndex(e => e.AttemptedAtUtc);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampRowVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampRowVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampRowVersions()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            var rowVersionProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(User.RowVersion)
                && p.Metadata.ClrType == typeof(byte[]));
            if (rowVersionProperty is not null)
            {
                rowVersionProperty.CurrentValue = Guid.NewGuid().ToByteArray();
            }
        }
    }
}
