using KhittoktunPortal.Api;
using KhittoktunPortal.Data;
using KhittoktunPortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var portalEnabled = builder.Configuration.GetValue("Portal:Enabled", false);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=khittoktun;Username=postgres;Password=CHANGE_ME";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login.html";
    options.AccessDeniedPath = "/access-denied.html";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("PortalUser", policy => policy.RequireRole("Admin", "Shareholder"));
});

var app = builder.Build();

if (portalEnabled)
{
    await SeedData.InitializeAsync(app.Services, app.Configuration, app.Environment);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.Use(async (context, next) =>
{
    if (!portalEnabled && IsPortalPath(context.Request.Path))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapPortalApi();
app.MapFallbackToFile("index.html");

app.Run();

static bool IsPortalPath(PathString path)
{
    return path.StartsWithSegments("/login.html")
        || path.StartsWithSegments("/portal.html")
        || path.StartsWithSegments("/admin.html")
        || path.StartsWithSegments("/change-password.html")
        || path.StartsWithSegments("/access-denied.html")
        || path.StartsWithSegments("/api/auth")
        || path.StartsWithSegments("/api/shareholder")
        || path.StartsWithSegments("/api/admin");
}
