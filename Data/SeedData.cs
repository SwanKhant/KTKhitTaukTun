using KhittoktunPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KhittoktunPortal.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var database = provider.GetRequiredService<ApplicationDbContext>();
        await database.Database.EnsureCreatedAsync();

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Shareholder" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
                }
            }
        }

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = configuration["Admin:Email"]
            ?? Environment.GetEnvironmentVariable("KHITTOKTUN_ADMIN_EMAIL")
            ?? "admin@alphainnovations.local";
        var adminPassword = configuration["Admin:Password"]
            ?? Environment.GetEnvironmentVariable("KHITTOKTUN_ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Set KHITTOKTUN_ADMIN_PASSWORD before starting the application outside Development.");
            }

            adminPassword = "ChangeMe123!";
            Console.WriteLine("WARNING: Development admin password is ChangeMe123!. Change it before deployment.");
        }

        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            // Preserve the existing development account when the company name changes.
            admin = await userManager.FindByEmailAsync("admin@alphainnovations.local");
            if (admin is not null)
            {
                admin.Email = adminEmail;
                admin.UserName = adminEmail;
                await userManager.UpdateAsync(admin);
            }
        }
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = "System Administrator"
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(x => x.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        if (!await database.FinancialSummaries.AnyAsync())
        {
            database.FinancialSummaries.Add(new FinancialSummary
            {
                TotalMoney = 1_485_250m,
                CurrentFyRevenue = 642_800m,
                LastFyRevenue = 542_000m,
                DueAmount = 18_500m,
                IsDuePaid = false,
                UpdatedAtUtc = DateTime.UtcNow,
                UpdatedByUserId = admin.Id
            });
        }

        if (!await database.RevenueHistory.AnyAsync())
        {
            database.RevenueHistory.AddRange(
                new RevenueHistory { FinancialYear = "FY 2021–22", Revenue = 285_000m, Status = "Audited", SortOrder = 1 },
                new RevenueHistory { FinancialYear = "FY 2022–23", Revenue = 348_500m, Status = "Audited", SortOrder = 2 },
                new RevenueHistory { FinancialYear = "FY 2023–24", Revenue = 419_200m, Status = "Audited", SortOrder = 3 },
                new RevenueHistory { FinancialYear = "FY 2024–25", Revenue = 476_000m, Status = "Audited", SortOrder = 4 },
                new RevenueHistory { FinancialYear = "FY 2025–26", Revenue = 542_000m, Status = "Closed", SortOrder = 5 },
                new RevenueHistory { FinancialYear = "FY 2026–27", Revenue = 642_800m, Status = "Active YTD", SortOrder = 6 });
        }

        await database.SaveChangesAsync();
    }
}
