using System.Globalization;
using System.Text;
using Duende.IdentityServer.Licensing;
using IdentityServerLogic;
using IdentityServerLogic.Identity;
using Serilog;
using Microsoft.EntityFrameworkCore;
using Duende.IdentityServer.EntityFramework.DbContexts;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    var app = builder
        .ConfigureLogging()
        .ConfigureServices()
        .ConfigurePipeline();

    // Migrate DB trước khi seed hoặc chạy web
    using (var scope = app.Services.CreateScope())
    {
        var sp = scope.ServiceProvider;

        // Duende stores
        await sp.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<ConfigurationDbContext>().Database.MigrateAsync();

        // ASP.NET Identity store
        await sp.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    }

    // ✅ Seed chỉ khi truyền tham số /seed
    if (args.Contains("/seed", StringComparer.OrdinalIgnoreCase))
    {
        Log.Information("Seeding database...");
        await SeedData.EnsureSeedData(app);
        Log.Information("Done seeding database. Exiting.");
        return;
    }

    if (app.Environment.IsDevelopment())
    {
        app.Lifetime.ApplicationStopping.Register(() =>
        {
            var usage = app.Services.GetRequiredService<LicenseUsageSummary>();
            Console.Write(Summary(usage));
        });
    }

    // Cho phép tắt redirect HTTPS khi chạy container
    var disableHttpsRedirect =
        Environment.GetEnvironmentVariable("DISABLE_HTTPS_REDIRECT")?.ToLower() == "true";

    if (!disableHttpsRedirect)
    {
        app.UseHttpsRedirection();
    }

    // Health endpoint đơn giản cho Docker healthcheck
    app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

    app.MapRestaurantAdminApi();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}

static string Summary(LicenseUsageSummary usage)
{
    var sb = new StringBuilder();
    sb.AppendLine("IdentityServer Usage Summary:");
    sb.AppendLine(CultureInfo.InvariantCulture, $"  License: {usage.LicenseEdition}");
    var features = usage.FeaturesUsed.Count > 0 ? string.Join(", ", usage.FeaturesUsed) : "None";
    sb.AppendLine(CultureInfo.InvariantCulture, $"  Business and Enterprise Edition Features Used: {features}");
    sb.AppendLine(CultureInfo.InvariantCulture, $"  {usage.ClientsUsed.Count} Client Id(s) Used");
    sb.AppendLine(CultureInfo.InvariantCulture, $"  {usage.IssuersUsed.Count} Issuer(s) Used");

    return sb.ToString();
}
