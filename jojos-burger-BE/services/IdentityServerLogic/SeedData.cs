using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Serilog;
using System.Security.Claims;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;

namespace IdentityServerLogic
{
    public static class SeedData
    {
        public static async Task EnsureSeedData(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var configDb = sp.GetRequiredService<ConfigurationDbContext>();
            var userMgr = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole>>();

            Log.Information("Seeding IdentityServer via WebApplication...");

            // ⚠️ Giả định DB đã được Migrate trước đó (Program.cs đã làm)
            // Xóa dữ liệu cũ
            configDb.Clients.RemoveRange(configDb.Clients);
            configDb.IdentityResources.RemoveRange(configDb.IdentityResources);
            configDb.ApiScopes.RemoveRange(configDb.ApiScopes);
            configDb.ApiResources.RemoveRange(configDb.ApiResources);
            await configDb.SaveChangesAsync();

            // Seed lại dữ liệu cấu hình IDS
            foreach (var res in Config.IdentityResources.ToList())
                configDb.IdentityResources.Add(res.ToEntity());
            foreach (var scopeDef in Config.ApiScopes.ToList())
                configDb.ApiScopes.Add(scopeDef.ToEntity());
            foreach (var apiRes in Config.ApiResources.ToList())
                configDb.ApiResources.Add(apiRes.ToEntity());
            foreach (var client in Config.Clients.ToList())
                configDb.Clients.Add(client.ToEntity());
            await configDb.SaveChangesAsync();

            Log.Information("Seeded configuration data.");

            // Tạo roles
            if (!await roleMgr.RoleExistsAsync("Admin"))
                await roleMgr.CreateAsync(new IdentityRole("Admin"));
            if (!await roleMgr.RoleExistsAsync("User"))
                await roleMgr.CreateAsync(new IdentityRole("User"));

            // USER ADMIN
            const string adminEmail = "admin@gmail.com";
            const string adminPassword = "123456";

            var existingAdmin = await userMgr.FindByEmailAsync(adminEmail);
            if (existingAdmin != null)
                await userMgr.DeleteAsync(existingAdmin);

            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var adminResult = await userMgr.CreateAsync(adminUser, adminPassword);
            if (adminResult.Succeeded)
            {
                await userMgr.AddToRoleAsync(adminUser, "Admin");
                await userMgr.AddClaimsAsync(adminUser, new[]
                {
                    new Claim("name", "Admin User"),
                    new Claim("preferred_username", "admin"),
                    new Claim("email", adminEmail),
                    new Claim("given_name", "Quan"),
                    new Claim("family_name", "Dang"),
                    new Claim("role", "Admin")
                });

                Log.Information("Created Admin user + claims: {Email}", adminEmail);
            }
            else
            {
                Log.Error("Error creating admin user: {Errors}",
                    string.Join(", ", adminResult.Errors.Select(e => e.Description)));
            }

            // USER THƯỜNG
            const string normalEmail = "123@gmail.com";
            const string normalPassword = "123456";

            var existingUser = await userMgr.FindByEmailAsync(normalEmail);
            if (existingUser != null)
                await userMgr.DeleteAsync(existingUser);

            var normalUser = new ApplicationUser
            {
                UserName = normalEmail,
                Email = normalEmail,
                EmailConfirmed = true
            };

            var userResult = await userMgr.CreateAsync(normalUser, normalPassword);
            if (userResult.Succeeded)
            {
                await userMgr.AddToRoleAsync(normalUser, "User");
                await userMgr.AddClaimsAsync(normalUser, new[]
                {
                    new Claim("name", "Normal User"),
                    new Claim("preferred_username", "dqdq"),
                    new Claim("email", normalEmail),
                    new Claim("given_name", "Quan"),
                    new Claim("family_name", "Dang"),
                    new Claim("role", "User")
                });

                Log.Information("Created Normal user + claims: {Email}", normalEmail);
            }
            else
            {
                Log.Error("Error creating normal user: {Errors}",
                    string.Join(", ", userResult.Errors.Select(e => e.Description)));
            }

            Log.Information("Seeding completed successfully!");
        }
    }
}
