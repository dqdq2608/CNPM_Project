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

    // ================================
    //  PostgreSQL: Tự migrate DB
    // ================================
    using (var scope = app.Services.CreateScope())
    {
        var sp = scope.ServiceProvider;

        await sp.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<ConfigurationDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    }

    // ================================
    //  Chỉ seed khi có ENV: RUN_SEED=true
    // ================================
    var seedFlag = Environment.GetEnvironmentVariable("RUN_SEED");
    if (seedFlag == "true")
    {
        Log.Information("Seeding database...");
        await SeedData.EnsureSeedData(app);
        Log.Information("Seeding done.");
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

    // Tắt HTTPS redirect nếu chạy container/cloud
    var disableHttpsRedirect =
        Environment.GetEnvironmentVariable("DISABLE_HTTPS_REDIRECT")?.ToLower() == "true";

    if (!disableHttpsRedirect)
    {
        app.UseHttpsRedirection();
    }

    // Healthcheck cho Render
    app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

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
