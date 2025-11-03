using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace IdentityServerLogic;

public class SeedData
{
    public static async Task EnsureSeedData(WebApplication app)
    {
        using (var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            // Migrate 2 DB của IdentityServer
            scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
            var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            context.Database.Migrate();

            // ✅ Migrate DB chứa bảng AspNetUsers
            var userContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            userContext.Database.Migrate();

            /// Xóa toàn bộ user cũ
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var users = userMgr.Users.ToList();
            foreach (var u in users)
            {
                await userMgr.DeleteAsync(u);
            }
            Log.Information("🗑️ Đã xóa {Count} user cũ", users.Count);

            // Tạo user mới
            var email = "123@gmail.com";
            var password = "123456";
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userMgr.CreateAsync(user, password);
            if (result.Succeeded)
            {
                Log.Information("✅ Đã tạo user mới: {Email}", email);
            }
            else
            {
                Log.Error("❌ Tạo user thất bại: {Error}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            // ✅ Tiếp tục seed Clients, Scopes, Resources
            EnsureSeedData(context);
        }
    }

    private static void EnsureSeedData(ConfigurationDbContext context)
    {
        foreach (var client in Config.Clients.ToList())
        {
            var existing = context.Clients
                .Include(x => x.RedirectUris)
                .Include(x => x.AllowedScopes)
                .Include(x => x.ClientSecrets)
                .FirstOrDefault(c => c.ClientId == client.ClientId);

            if (existing == null)
            {
                Log.Information("Adding client {ClientId}", client.ClientId);
                context.Clients.Add(client.ToEntity());
            }
            else
            {
                Log.Information("Updating client {ClientId}", client.ClientId);
                context.Clients.Remove(existing);
                context.Clients.Add(client.ToEntity());
            }
        }
        context.SaveChanges();

        if (!context.IdentityResources.Any())
        {
            Log.Debug("IdentityResources being populated");
            foreach (var resource in Config.IdentityResources.ToList())
            {
                context.IdentityResources.Add(resource.ToEntity());
            }
            context.SaveChanges();
        }

        foreach (var s in Config.ApiScopes.ToList())
        {
            var exists = context.ApiScopes.Any(x => x.Name == s.Name);
            if (!exists)
            {
                Log.Information("Adding ApiScope {Scope}", s.Name);
                context.ApiScopes.Add(s.ToEntity());
            }
        }
        context.SaveChanges();
    }
}
