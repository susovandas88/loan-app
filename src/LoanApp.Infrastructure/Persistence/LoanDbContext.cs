using LoanApp.Application.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LoanApp.Infrastructure.Persistence;

public sealed class LoanDbContext : DbContext
{
    public LoanDbContext(DbContextOptions<LoanDbContext> options) : base(options)
    {
    }

    public DbSet<LoanApplication> Applications => Set<LoanApplication>();
    public DbSet<LoanDocument> Documents => Set<LoanDocument>();
    public DbSet<VerificationFinding> Findings => Set<VerificationFinding>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Guid>().HaveConversion<GuidToStringConverter>();
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoanApplication>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.ApplicantUserId).HasMaxLength(128).IsRequired();
            b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            b.Property(x => x.Email).HasMaxLength(256).IsRequired();
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.MonthlyIncome).HasPrecision(18, 2);
            b.Property(x => x.DecisionEngine).HasMaxLength(64).IsRequired();
            b.Property(x => x.StatusReasonCode).HasMaxLength(64);
            b.Property(x => x.BankReference).HasMaxLength(128);
            b.HasIndex(x => x.ApplicantUserId);
            b.HasMany(x => x.Documents).WithOne(x => x.Application).HasForeignKey(x => x.ApplicationId);
            b.HasMany(x => x.Findings).WithOne(x => x.Application).HasForeignKey(x => x.ApplicationId);
            b.ToTable(t => t.UseSqlReturningClause(false));
        });

        modelBuilder.Entity<LoanDocument>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.BlobPath).HasMaxLength(512).IsRequired();
            b.Property(x => x.FileName).HasMaxLength(256).IsRequired();
            b.Property(x => x.ContentType).HasMaxLength(128);
            b.Property(x => x.ExtractedJson);
            b.ToTable(t => t.UseSqlReturningClause(false));
        });

        modelBuilder.Entity<AppUser>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(64);
            b.Property(x => x.Email).HasMaxLength(256).IsRequired();
            b.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            b.Property(x => x.PasswordSalt).HasMaxLength(128).IsRequired();
            b.Property(x => x.PasswordHash).HasMaxLength(128).IsRequired();
            b.HasIndex(x => x.Email).IsUnique();
            b.ToTable("Users");
        });

        modelBuilder.Entity<VerificationFinding>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.Code).HasMaxLength(64).IsRequired();
            b.Property(x => x.Message).HasMaxLength(512).IsRequired();
            b.Property(x => x.Engine).HasMaxLength(64).IsRequired();
            b.ToTable(t => t.UseSqlReturningClause(false));
        });
    }
}

file sealed class UtcDateTimeOffsetConverter() : ValueConverter<DateTimeOffset, DateTime>(
    v => v.UtcDateTime,
    v => new DateTimeOffset(DateTime.SpecifyKind(v, DateTimeKind.Utc)));
