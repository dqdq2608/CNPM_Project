using System.Globalization;
using Duende.IdentityServer;
using IdentityServerLogic;
using IdentityServerLogic.Pages.Admin.ApiScopes;
using IdentityServerLogic.Pages.Admin.Clients;
using IdentityServerLogic.Pages.Admin.IdentityScopes;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Filters;
using Microsoft.AspNetCore.Identity;

// 🔥 thêm using cho EF Identity (nếu bạn để ApplicationDbContext/ApplicationUser trong root project thì không cần namespace khác)

namespace IdentityServerLogic;

internal static class HostingExtensions
{
    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, lc) =>
        {
            lc.WriteTo.Logger(consoleLogger =>
            {
                consoleLogger.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                    formatProvider: CultureInfo.InvariantCulture);
                if (builder.Environment.IsDevelopment())
                {
                    consoleLogger.Filter.ByExcluding(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary"));
                }
            });
            if (builder.Environment.IsDevelopment())
            {
                lc.WriteTo.Logger(fileLogger =>
                {
                    fileLogger
                        .WriteTo.File("./diagnostics/diagnostic.log", rollingInterval: RollingInterval.Day,
                            fileSizeLimitBytes: 1024 * 1024 * 10,
                            rollOnFileSizeLimit: true,
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                            formatProvider: CultureInfo.InvariantCulture)
                        .Filter
                        .ByIncludingOnly(Matching.FromSource("Duende.IdentityServer.Diagnostics.Summary"));
                }).Enrich.FromLogContext().ReadFrom.Configuration(ctx.Configuration);
            }
        });
        return builder;
    }

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();

        //  dùng đúng connection string bạn đã cấu hình (SQLite/SQL Server đều OK)
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        // =========================
        //  ASP.NET Identity (USER STORE)
        // =========================
        builder.Services.AddDbContext<ApplicationDbContext>(o =>
            o.UseSqlite(connectionString)); // nếu dùng SQL Server: o.UseSqlServer(connectionString)

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        
        builder.Services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
        });


        // =========================
        // IdentityServer + EF stores
        // =========================
        var isBuilder = builder.Services
            .AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                if (builder.Environment.IsDevelopment())
                {
                    options.Diagnostics.ChunkSize = 1024 * 1024 * 10;
                }
            })
            //  bỏ TestUsers, thay bằng ASP.NET Identity
            //.AddTestUsers(TestUsers.Users)
            .AddAspNetIdentity<ApplicationUser>() // 🔥 bắt buộc để ROPC validate username/password từ DB

            // config data từ DB (clients, resources, cors)
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = b =>
                    b.UseSqlite(connectionString, dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));
            })
            // operational data (codes, tokens, consents)
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = b =>
                    b.UseSqlite(connectionString, dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));
            })
            .AddLicenseSummary();

        // (giữ OIDC external demo nếu bạn muốn)
        builder.Services.AddAuthentication()
            .AddOpenIdConnect("oidc", "Sign-in with demo.duendesoftware.com", options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.SignOutScheme = IdentityServerConstants.SignoutScheme;
                options.SaveTokens = true;

                options.Authority = "https://demo.duendesoftware.com";
                options.ClientId = "interactive.confidential";
                options.ClientSecret = "secret";
                options.ResponseType = "code";

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        // simple admin/config pages
        {
            builder.Services.AddAuthorization(options =>
                options.AddPolicy("admin", policy => policy.RequireClaim("sub", "1"))
            );

            builder.Services.Configure<RazorPagesOptions>(options =>
                options.Conventions.AuthorizeFolder("/Admin", "admin"));

            builder.Services.AddTransient<IdentityServerLogic.Pages.Portal.ClientRepository>();
            builder.Services.AddTransient<ClientRepository>();
            builder.Services.AddTransient<IdentityScopeRepository>();
            builder.Services.AddTransient<ApiScopeRepository>();
        }

        // server-side sessions (tuỳ chọn)
        //isBuilder.AddServerSideSessions();
        //builder.Services.Configure<RazorPagesOptions>(options =>
        //    options.Conventions.AuthorizeFolder("/ServerSideSessions", "admin"));

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseIdentityServer();
        app.UseAuthorization();

        app.MapRazorPages().RequireAuthorization();
        return app;
    }
}
