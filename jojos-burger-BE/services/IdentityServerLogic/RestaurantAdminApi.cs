using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Serilog;
using IdentityServerLogic.Identity; // ApplicationUser

namespace IdentityServerLogic;

public static class RestaurantAdminApi
{
    private const string DefaultPassword = "123456";

    public static IEndpointRouteBuilder MapRestaurantAdminApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/restaurant-admins");

        // Tạo admin mới
        api.MapPost("/", CreateRestaurantAdmin);

        // ⭐ THÊM API XOÁ USER THEO EMAIL
        api.MapDelete("/by-email/{email}", DeleteRestaurantAdminByEmail);

        return app;
    }

    // ============================
    // ⭐ 1) XOÁ TÀI KHOẢN ADMIN
    // ============================
    private static async Task<IResult> DeleteRestaurantAdminByEmail(
        string email,
        UserManager<ApplicationUser> userMgr)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Results.BadRequest("Email is required.");

        var user = await userMgr.FindByEmailAsync(email);

        if (user is null)
            return Results.NotFound($"User with email {email} not found.");

        var result = await userMgr.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Results.BadRequest($"Delete failed: {errors}");
        }

        Log.Information("Deleted RestaurantAdmin {Email}", email);
        return Results.NoContent();
    }

    // ============================
    // ⭐ 2) TẠO TÀI KHOẢN ADMIN
    // ============================
    private static async Task<IResult> CreateRestaurantAdmin(
        CreateRestaurantAdminRequest request,
        UserManager<ApplicationUser> userMgr,
        RoleManager<IdentityRole> roleMgr)
    {
        if (!await roleMgr.RoleExistsAsync("RestaurantAdmin"))
        {
            await roleMgr.CreateAsync(new IdentityRole("RestaurantAdmin"));
        }

        var existing = await userMgr.FindByEmailAsync(request.Email);
        if (existing != null)
        {
            return Results.BadRequest($"User with email {request.Email} already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FullName = $"Owner {request.RestaurantName}",
            UserType = "RestaurantAdmin",
            RestaurantId = request.RestaurantId,
            RestaurantName = request.RestaurantName
        };

        var result = await userMgr.CreateAsync(user, DefaultPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            Log.Error("Error creating restaurant admin {Email}: {Errors}", request.Email, errors);
            return Results.BadRequest(errors);
        }

        await userMgr.AddToRoleAsync(user, "RestaurantAdmin");

        Log.Information("Created RestaurantAdmin {Email} for {RestaurantId}", request.Email, request.RestaurantId);

        return Results.Ok(new
        {
            Email = request.Email,
            Password = DefaultPassword,
            request.RestaurantId,
            request.RestaurantName
        });
    }
}
