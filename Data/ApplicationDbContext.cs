using KhittoktunPortal.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KhittoktunPortal.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<FinancialSummary> FinancialSummaries => Set<FinancialSummary>();
    public DbSet<RevenueHistory> RevenueHistory => Set<RevenueHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    private DbSet<T> Set<T>()
    {
        throw new NotImplementedException();
    }


    public DbSet<ShareholderFinancialRecord> ShareholderFinancialRecords => Set<ShareholderFinancialRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<FinancialSummary>().Property(x => x.TotalMoney).HasPrecision(18, 2);
        builder.Entity<FinancialSummary>().Property(x => x.CurrentFyRevenue).HasPrecision(18, 2);
        builder.Entity<FinancialSummary>().Property(x => x.LastFyRevenue).HasPrecision(18, 2);
        builder.Entity<FinancialSummary>().Property(x => x.DueAmount).HasPrecision(18, 2);
        builder.Entity<RevenueHistory>().Property(x => x.Revenue).HasPrecision(18, 2);
        builder.Entity<ShareholderFinancialRecord>().Property(x => x.TotalMoney).HasPrecision(18, 2);
        builder.Entity<ShareholderFinancialRecord>().Property(x => x.Revenue).HasPrecision(18, 2);
        builder.Entity<ShareholderFinancialRecord>().Property(x => x.DueAmount).HasPrecision(18, 2);
        builder.Entity<ShareholderFinancialRecord>()
            .HasIndex(x => new { x.ShareholderId, x.FinancialYear })
            .IsUnique();
    }
}

public class DbSet<T>
{
}


public class IdentityDbContext<T>
{
}