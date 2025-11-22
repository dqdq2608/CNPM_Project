using System.Globalization;
using System.Text;
using Duende.IdentityServer.Licensing;
using IdentityServerLogic;
using IdentityServerLogic.Identity;
using Serilog;
using Microsoft.EntityFrameworkCore;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.AspNetCore.DataProtection;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 🔥 Disable key rotation (Duende CE cannot handle this on Render)
    builder.Services.AddIdentityServer(options =>
    {
        options.KeyManagement.Enabled = false;
    });

    // 🔥 Persist DataProtection keys
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
        .SetApplicationName("ids");

    var app = builder
        .ConfigureLogging()
        .ConfigureServices()
        .ConfigurePipeline();

    // 🔥 Migrate DB (only migrations)
    using (var scope = app.Services.CreateScope())
    {
        var sp = scope.ServiceProvider;

        await sp.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<ConfigurationDbContext>().Database.MigrateAsync();
        await sp.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
    }

    // 🔥 Seed ONLY when explicitly called: `dotnet IdentityServerLogic.dll /seed`
    if (args.Contains("/seed", StringComparer.OrdinalIgnoreCase))
    {
        Log.Information("Seeding database...");
        await SeedData.EnsureSeedData(app);
        Log.Information("Done seeding database. Exiting.");
        return;
    }

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
