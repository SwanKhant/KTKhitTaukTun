<<<<<<< HEAD
# Khittoktun Portal

This is a company portfolio and private shareholder portal built with a static HTML/CSS/JavaScript frontend, ASP.NET Core .NET APIs, ASP.NET Core Identity, Entity Framework Core, and PostgreSQL.

## Run locally

The project uses the installed .NET 9 SDK and a local PostgreSQL server. Create a database named `khittoktun`, then update the password in `appsettings.json` or set the connection string through an environment variable:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=khittoktun;Username=postgres;Password=YourPostgresPassword"
```

From this folder:

```powershell
dotnet restore
dotnet run --launch-profile KhittoktunPortal
```

Open the HTTPS URL shown in the terminal.

The portal supports English and Myanmar. Use the `EN / မြန်မာ` language switcher in the header; your choice is saved in the browser.

Development admin login:

- Email: `admin@khittoktun.local`
- Password: `ChangeMe123!`

Change this password immediately. For a safer local password, set it before running:

```powershell
$env:KHITTOKTUN_ADMIN_PASSWORD = "YourStrongPasswordHere!"
dotnet run
```

The PostgreSQL database schema and starter data are created automatically when the application starts. The previous SQLite file in `App_Data` is not used by the application.

The portal is disabled by default for the first public production release. This allows the public website to run without a database connection. Set `Portal__Enabled=true` in the environment when the login, admin dashboard, CSV import, and PostgreSQL features are ready.

## Portal behavior

- `/` and `/index.html` are the public static company portfolio.
- `/login.html` is the login page.
- `/admin.html` is the admin dashboard.
- `/portal.html` is the shareholder dashboard.
- `/change-password.html` handles password changes.
- `/api` contains the .NET JSON endpoints used by the frontend.
- Shareholders are read-only; admins can update summaries and create accounts.
- Admins can import financial records from CSV.
- Every admin financial update and CSV import is recorded in the audit log.

## CSV import

Use the admin dashboard to upload a CSV with these columns:

```text
Email,DisplayName,TemporaryPassword,FinancialYear,TotalMoney,Revenue,DueAmount,IsDuePaid
```

Existing users are updated. New users are created as shareholders when a temporary password is supplied. Each user can have one financial row per financial year, so new years preserve history.

## Before production

1. Replace the development admin password with `KHITTOKTUN_ADMIN_PASSWORD`.
2. Replace the public contact behavior in `wwwroot/index.html` with the company email or inquiry API.
3. Enable HTTPS and use Azure Database for PostgreSQL in production.
4. Add email delivery for invitations and password resets.
5. Enable MFA for administrators.
6. Review the financial data model and CSV validation rules before importing production data.

The active frontend is static HTML/CSS/JavaScript. The application does not map Razor Pages.
=======
# KTKhitTaukTun
>>>>>>> 6f019b73c5af62e882e10f1766697b100dcc77f1
