using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Serilog;
using System.Security.Claims;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using IdentityServerLogic;

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

            Log.Information("Seeding IdentityServer via WebApplication...");

            // Xóa dữ liệu cũ
            configDb.Clients.RemoveRange(configDb.Clients);
            configDb.IdentityResources.RemoveRange(configDb.IdentityResources);
            configDb.ApiScopes.RemoveRange(configDb.ApiScopes);
            configDb.ApiResources.RemoveRange(configDb.ApiResources);
            await configDb.SaveChangesAsync();

            // Seed lại dữ liệu
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

            // Seed user mặc định
            const string email = "123@gmail.com";
            const string password = "123456";

            var existingUser = await userMgr.FindByEmailAsync(email);
            if (existingUser != null)
                await userMgr.DeleteAsync(existingUser);

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userMgr.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userMgr.AddClaimsAsync(user, new[]
                {
                    new Claim("name", "dqdq"),
                    new Claim("preferred_username", email),
                    new Claim("email", email),
                    new Claim("given_name", "Quan"),
                    new Claim("family_name", "Dang")
                });

                Log.Information("Created user + claims: {Email}", email);
            }
            else
            {
                Log.Error("Error creating user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            Log.Information("✅ Seeding completed!");
        }
    }
}
