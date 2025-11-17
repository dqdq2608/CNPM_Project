using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using IdentityServerLogic.Identity;

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

            Log.Information("Seeding IdentityServer...");

            // 1. Seed cấu hình IdentityServer
            configDb.Clients.RemoveRange(configDb.Clients);
            configDb.IdentityResources.RemoveRange(configDb.IdentityResources);
            configDb.ApiScopes.RemoveRange(configDb.ApiScopes);
            configDb.ApiResources.RemoveRange(configDb.ApiResources);
            await configDb.SaveChangesAsync();

            foreach (var res in Config.IdentityResources)
                configDb.IdentityResources.Add(res.ToEntity());
            foreach (var scopeDef in Config.ApiScopes)
                configDb.ApiScopes.Add(scopeDef.ToEntity());
            foreach (var apiRes in Config.ApiResources)
                configDb.ApiResources.Add(apiRes.ToEntity());
            foreach (var client in Config.Clients)
                configDb.Clients.Add(client.ToEntity());

            await configDb.SaveChangesAsync();
            Log.Information("Seeded IdentityServer configuration data.");

            // 2. Roles
            var roles = new[] { "RestaurantAdmin", "Customer" };

            foreach (var role in roles)
            {
                if (!await roleMgr.RoleExistsAsync(role))
                {
                    await roleMgr.CreateAsync(new IdentityRole(role));
                }
            }

            // 3. Seed Restaurant Admin users
            var restaurants = new[]
            {
                new { Id = "rest-001", Name = "Jojo Burger Q1" },
                new { Id = "rest-002", Name = "Jojo Burger Q3" },
                new { Id = "rest-003", Name = "Jojo Burger Q7" }
            };

            foreach (var r in restaurants)
            {
                var email = $"owner.{r.Id}@gmail.com";

                var existing = await userMgr.FindByEmailAsync(email);
                if (existing != null)
                {
                    await userMgr.DeleteAsync(existing);
                }

                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = $"Owner {r.Name}",
                    UserType = "RestaurantAdmin",
                    RestaurantId = r.Id,
                    RestaurantName = r.Name
                };

                var result = await userMgr.CreateAsync(user, "123456");
                if (!result.Succeeded)
                {
                    Log.Error("Error creating restaurant admin {Email}: {Errors}",
                        email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    continue;
                }

                await userMgr.AddToRoleAsync(user, "RestaurantAdmin");

                Log.Information("Created RestaurantAdmin {Email} for {RestaurantId}", email, r.Id);
            }

            // 4. Seed Customer users
            var customers = new[]
            {
                new { Email = "customer1@gmail.com", Name = "Customer 1" },
                new { Email = "customer2@gmail.com", Name = "Customer 2" },
                new { Email = "customer3@gmail.com", Name = "Customer 3" }
            };

            foreach (var c in customers)
            {
                var existing = await userMgr.FindByEmailAsync(c.Email);
                if (existing != null)
                {
                    await userMgr.DeleteAsync(existing);
                }

                var user = new ApplicationUser
                {
                    UserName = c.Email,
                    Email = c.Email,
                    EmailConfirmed = true,
                    FullName = c.Name,
                    UserType = "Customer",
                    RestaurantId = null,
                    RestaurantName = null
                };

                var result = await userMgr.CreateAsync(user, "123456");
                if (!result.Succeeded)
                {
                    Log.Error("Error creating customer {Email}: {Errors}",
                        c.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                    continue;
                }

                await userMgr.AddToRoleAsync(user, "Customer");

                Log.Information("Created Customer {Email}", c.Email);
            }

            Log.Information("Seeding IdentityServer done.");
        }
    }
}
