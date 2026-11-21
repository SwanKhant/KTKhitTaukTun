using System.Globalization;
using System.Security.Claims;
using KhittoktunPortal.Data;
using KhittoktunPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KhittoktunPortal.Api;

public static class PortalApi
{
    public static void MapPortalApi(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/config", (IConfiguration configuration) =>
            Results.Ok(new { portalEnabled = configuration.GetValue<bool>("Portal:Enabled") }));
        api.MapGet("/auth/me", GetCurrentUserAsync);
        api.MapPost("/auth/login", LoginAsync);
        api.MapPost("/auth/logout", LogoutAsync).RequireAuthorization();
        api.MapPost("/auth/change-password", ChangePasswordAsync).RequireAuthorization();

        api.MapGet("/shareholder/dashboard", GetShareholderDashboardAsync)
            .RequireAuthorization("PortalUser");

        api.MapGet("/admin/dashboard", GetAdminDashboardAsync)
            .RequireAuthorization("AdminOnly");
        api.MapPut("/admin/financial-summary", UpdateFinancialSummaryAsync)
            .RequireAuthorization("AdminOnly");
        api.MapGet("/admin/users", GetUsersAsync)
            .RequireAuthorization("AdminOnly");
        api.MapPost("/admin/users", CreateUserAsync)
            .RequireAuthorization("AdminOnly");
        api.MapPost("/admin/import-financials", ImportFinancialsAsync)
            .RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return Results.Ok(new { authenticated = false });
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Results.Ok(new { authenticated = false });
        }

        var roles = await userManager.GetRolesAsync(user);
        return Results.Ok(new
        {
            authenticated = true,
            email = user.Email,
            displayName = user.DisplayName,
            roles,
            isAdmin = roles.Contains("Admin"),
            isShareholder = roles.Contains("Shareholder")
        });
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { message = "Email and password are required." });
        }

        var email = request.Email.Trim();
        var result = await signInManager.PasswordSignInAsync(
            email,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Results.Json(new { message = "Account is temporarily locked." }, statusCode: StatusCodes.Status423Locked);
        }

        if (!result.Succeeded)
        {
            return Results.Unauthorized();
        }

        var user = await userManager.FindByEmailAsync(email);
        var roles = user is null ? [] : await userManager.GetRolesAsync(user);
        var redirect = roles.Contains("Admin")
            ? "/admin.html"
            : roles.Contains("Shareholder") ? "/portal.html" : "/";

        return Results.Ok(new { redirect });
    }

    private static async Task<IResult> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
            string.IsNullOrWhiteSpace(request.NewPassword) ||
            request.NewPassword != request.ConfirmPassword)
        {
            return Results.BadRequest(new { message = "Password fields are required and must match." });
        }

        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return Results.BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) });
        }

        return Results.Ok(new { message = "Password changed." });
    }

    private static async Task<IResult> GetShareholderDashboardAsync(
        ClaimsPrincipal principal,
        ApplicationDbContext database,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        var summary = await database.FinancialSummaries.AsNoTracking().SingleAsync();
        var history = await database.RevenueHistory.AsNoTracking().OrderBy(x => x.SortOrder).ToListAsync();
        var records = user is null
            ? []
            : await database.ShareholderFinancialRecords.AsNoTracking()
                .Where(x => x.ShareholderId == user.Id)
                .OrderByDescending(x => x.FinancialYear)
                .ToListAsync();

        return Results.Ok(new { summary, history, records });
    }

    private static async Task<IResult> GetAdminDashboardAsync(ApplicationDbContext database)
    {
        var summary = await database.FinancialSummaries.AsNoTracking().SingleAsync();
        var auditLogs = await database.AuditLogs.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync();

        return Results.Ok(new { summary, auditLogs });
    }

    private static async Task<IResult> UpdateFinancialSummaryAsync(
        FinancialSummaryRequest request,
        ClaimsPrincipal principal,
        ApplicationDbContext database,
        UserManager<ApplicationUser> userManager)
    {
        if (request.TotalMoney < 0 || request.CurrentFyRevenue < 0 || request.LastFyRevenue < 0 || request.DueAmount < 0)
        {
            return Results.BadRequest(new { message = "Financial values cannot be negative." });
        }

        var summary = await database.FinancialSummaries.SingleAsync();
        summary.TotalMoney = request.TotalMoney;
        summary.CurrentFyRevenue = request.CurrentFyRevenue;
        summary.LastFyRevenue = request.LastFyRevenue;
        summary.DueAmount = request.DueAmount;
        summary.IsDuePaid = request.IsDuePaid;
        summary.UpdatedAtUtc = DateTime.UtcNow;
        summary.UpdatedByUserId = userManager.GetUserId(principal);

        database.AuditLogs.Add(new AuditLog
        {
            Action = "Financial summary updated",
            UserId = summary.UpdatedByUserId,
            Details = $"Total={request.TotalMoney:C2}; CurrentFY={request.CurrentFyRevenue:C2}; DuePaid={request.IsDuePaid}",
            CreatedAtUtc = DateTime.UtcNow
        });

        await database.SaveChangesAsync();
        return Results.Ok(summary);
    }

    private static async Task<IResult> GetUsersAsync(UserManager<ApplicationUser> userManager)
    {
        var users = new List<UserResponse>();
        foreach (var user in await userManager.Users.OrderBy(x => x.Email).ToListAsync())
        {
            var roles = await userManager.GetRolesAsync(user);
            users.Add(new UserResponse(user.Id, user.DisplayName, user.Email ?? string.Empty, roles));
        }

        return Results.Ok(users);
    }

    private static async Task<IResult> CreateUserAsync(
        CreateUserRequest request,
        UserManager<ApplicationUser> userManager)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.TemporaryPassword))
        {
            return Results.BadRequest(new { message = "Display name, email, and temporary password are required." });
        }

        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = request.DisplayName.Trim()
        };

        var result = await userManager.CreateAsync(user, request.TemporaryPassword);
        if (!result.Succeeded)
        {
            return Results.BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) });
        }

        await userManager.AddToRoleAsync(user, "Shareholder");
        return Results.Ok(new UserResponse(user.Id, user.DisplayName, user.Email ?? email, ["Shareholder"]));
    }

    private static async Task<IResult> ImportFinancialsAsync(
        HttpRequest request,
        ClaimsPrincipal principal,
        ApplicationDbContext database,
        UserManager<ApplicationUser> userManager)
    {
        var form = await request.ReadFormAsync();
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { message = "Choose a CSV file first." });
        }

        var rows = await CsvReader.ReadAsync(file.OpenReadStream());
        if (rows.Count == 0)
        {
            return Results.BadRequest(new { message = "The CSV file has no data rows." });
        }

        await using var transaction = await database.Database.BeginTransactionAsync();
        var imported = 0;

        try
        {
            foreach (var row in rows)
            {
                var email = Required(row, "Email");
                var financialYear = Required(row, "FinancialYear");
                var user = await userManager.FindByEmailAsync(email);

                if (user is null)
                {
                    var temporaryPassword = Required(row, "TemporaryPassword");
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        DisplayName = row.GetValueOrDefault("DisplayName")?.Trim() ?? email
                    };

                    var createResult = await userManager.CreateAsync(user, temporaryPassword);
                    if (!createResult.Succeeded)
                    {
                        throw new InvalidOperationException(string.Join(" ", createResult.Errors.Select(x => x.Description)));
                    }

                    await userManager.AddToRoleAsync(user, "Shareholder");
                }

                if (!string.IsNullOrWhiteSpace(row.GetValueOrDefault("DisplayName")))
                {
                    user.DisplayName = row["DisplayName"].Trim();
                }

                var record = await database.ShareholderFinancialRecords
                    .SingleOrDefaultAsync(x => x.ShareholderId == user.Id && x.FinancialYear == financialYear);

                if (record is null)
                {
                    record = new ShareholderFinancialRecord
                    {
                        ShareholderId = user.Id,
                        FinancialYear = financialYear
                    };
                    database.ShareholderFinancialRecords.Add(record);
                }

                record.TotalMoney = DecimalValue(row, "TotalMoney");
                record.Revenue = DecimalValue(row, "Revenue");
                record.DueAmount = DecimalValue(row, "DueAmount");
                record.IsDuePaid = BoolValue(row, "IsDuePaid");
                record.ImportedAtUtc = DateTime.UtcNow;
                record.ImportedByUserId = userManager.GetUserId(principal);
                imported++;
            }

            database.AuditLogs.Add(new AuditLog
            {
                Action = "Financial CSV imported",
                UserId = userManager.GetUserId(principal),
                Details = $"File={file.FileName}; Rows={imported}",
                CreatedAtUtc = DateTime.UtcNow
            });

            await database.SaveChangesAsync();
            await transaction.CommitAsync();
            return Results.Ok(new { imported, message = $"Imported {imported} row(s)." });
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException)
        {
            await transaction.RollbackAsync();
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    private static string Required(IReadOnlyDictionary<string, string> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"CSV column '{key}' is required.");
        }

        return value.Trim();
    }

    private static decimal DecimalValue(IReadOnlyDictionary<string, string> row, string key)
    {
        var value = row.GetValueOrDefault(key);
        return string.IsNullOrWhiteSpace(value)
            ? 0
            : decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static bool BoolValue(IReadOnlyDictionary<string, string> row, string key)
    {
        var value = row.GetValueOrDefault(key);
        return !string.IsNullOrWhiteSpace(value) &&
            (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value == "1");
    }

    private sealed record LoginRequest(string Email, string Password, bool RememberMe = false);
    private sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
    private sealed record FinancialSummaryRequest(decimal TotalMoney, decimal CurrentFyRevenue, decimal LastFyRevenue, decimal DueAmount, bool IsDuePaid);
    private sealed record CreateUserRequest(string DisplayName, string Email, string TemporaryPassword);
    private sealed record UserResponse(string Id, string DisplayName, string Email, IList<string> Roles);

    private static class CsvReader
    {
        public static async Task<List<Dictionary<string, string>>> ReadAsync(Stream stream)
        {
            using var reader = new StreamReader(stream);
            var headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                return [];
            }

            var headers = Split(headerLine).Select(x => x.Trim()).ToArray();
            var rows = new List<Dictionary<string, string>>();
            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var values = Split(line);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < headers.Length; index++)
                {
                    row[headers[index]] = index < values.Count ? values[index].Trim() : string.Empty;
                }

                rows.Add(row);
            }

            return rows;
        }

        private static List<string> Split(string line)
        {
            var values = new List<string>();
            var current = new System.Text.StringBuilder();
            var quoted = false;

            for (var index = 0; index < line.Length; index++)
            {
                var character = line[index];
                if (character == '"')
                {
                    if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        current.Append('"');
                        index++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (character == ',' && !quoted)
                {
                    values.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(character);
                }
            }

            values.Add(current.ToString());
            return values;
        }
    }
}
