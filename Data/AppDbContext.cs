using Microsoft.EntityFrameworkCore;
using PocGestorExpectativas.Models;

namespace PocGestorExpectativas.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments { get; set; }
    public DbSet<Expectation> Expectations { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurações para Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IdentificationField).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BeneficiaryName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.NormalizedBeneficiary).HasMaxLength(200);
            entity.Property(e => e.Value).HasPrecision(18, 2);
            
            entity.HasIndex(e => e.IdentificationField);
            entity.HasIndex(e => e.NormalizedBeneficiary);
            entity.HasIndex(e => e.Pago);
        });

        // Configurações para Expectation
        modelBuilder.Entity<Expectation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NormalizedBeneficiary).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Rationale).HasMaxLength(1000);
            entity.Property(e => e.AnalysisMethod).HasMaxLength(50);
            entity.Property(e => e.NextExpectedAmount).HasPrecision(18, 2);
            
            entity.HasIndex(e => e.NormalizedBeneficiary);
            entity.HasIndex(e => e.NextExpectedPaymentDate);
        });

        // Configurações para AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Details).HasMaxLength(2000);
            
            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.ExpectationId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}
