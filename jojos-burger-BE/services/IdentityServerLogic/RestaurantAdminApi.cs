using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;          // ToListAsync
using Serilog;
using IdentityServerLogic.Identity;          // ApplicationUser

namespace IdentityServerLogic;

public static class RestaurantAdminApi
{
    private const string DefaultPassword = "123456";

    public static IEndpointRouteBuilder MapRestaurantAdminApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/restaurant-admins");

        // 1) Tạo admin mới
        api.MapPost("/", CreateRestaurantAdmin);

        // 2) Lấy admin theo RestaurantId (Catalog hiển thị email)
        //    GET /api/restaurant-admins/by-restaurant/{restaurantId}
        api.MapGet("/by-restaurant/{restaurantId}", GetAdminsByRestaurant);

        // 3) Xoá admin theo RestaurantId (Catalog gọi khi hard delete nhà hàng)
        //    DELETE /api/restaurant-admins/by-restaurant/{restaurantId}
        api.MapDelete("/by-restaurant/{restaurantId}", DeleteRestaurantAdminsByRestaurant);

        // 4) Deactivate admin khi nhà hàng đóng cửa / soft delete
        //    POST /api/restaurant-admins/by-restaurant/{restaurantId}/deactivate
        api.MapPost("/by-restaurant/{restaurantId}/deactivate", DeactivateAdminsByRestaurant);

        // 5) Activate admin khi nhà hàng mở lại
        //    POST /api/restaurant-admins/by-restaurant/{restaurantId}/activate
        api.MapPost("/by-restaurant/{restaurantId}/activate", ActivateAdminsByRestaurant);

        return app;
    }

    // ========================= 2) LẤY ADMIN THEO RESTAURANT =========================
    private static async Task<IResult> GetAdminsByRestaurant(
        string restaurantId,
        UserManager<ApplicationUser> userMgr)
    {
        if (string.IsNullOrWhiteSpace(restaurantId))
            return Results.BadRequest("RestaurantId is required.");

        var users = await userMgr.Users
            .Where(u => u.RestaurantId == restaurantId && u.UserType == "RestaurantAdmin")
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.RestaurantId,
                u.RestaurantName
            })
            .ToListAsync();

        if (users.Count == 0)
            return Results.NotFound();

        return Results.Ok(users);
    }

    // ========================= 3) XOÁ ADMIN THEO RESTAURANT =========================
    private static async Task<IResult> DeleteRestaurantAdminsByRestaurant(
        string restaurantId,
        UserManager<ApplicationUser> userMgr)
    {
        if (string.IsNullOrWhiteSpace(restaurantId))
            return Results.BadRequest("RestaurantId is required.");

        var users = await userMgr.Users
            .Where(u => u.RestaurantId == restaurantId && u.UserType == "RestaurantAdmin")
            .ToListAsync();

        if (users.Count == 0)
            return Results.NotFound($"No RestaurantAdmin for restaurant {restaurantId}.");

        foreach (var user in users)
        {
            var result = await userMgr.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                return Results.BadRequest($"Delete failed for {user.Email}: {errors}");
            }

            Log.Information("Deleted RestaurantAdmin {Email} for Restaurant {RestaurantId}",
                user.Email, restaurantId);
        }

        return Results.NoContent();
    }

    // ========================= 1) TẠO TÀI KHOẢN ADMIN =========================
    private static async Task<IResult> CreateRestaurantAdmin(
        CreateRestaurantAdminRequest request,
        UserManager<ApplicationUser> userMgr,
        RoleManager<IdentityRole> roleMgr)
    {
        // Đảm bảo role tồn tại
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
            RestaurantName = request.RestaurantName,
            IsRestaurantActive = true,          // active mặc định
            RestaurantStatus = "Active"
        };

        var result = await userMgr.CreateAsync(user, DefaultPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            Log.Error("Error creating restaurant admin {Email}: {Errors}", request.Email, errors);
            return Results.BadRequest(errors);
        }

        await userMgr.AddToRoleAsync(user, "RestaurantAdmin");

        Log.Information("Created RestaurantAdmin {Email} for {RestaurantId}",
            request.Email, request.RestaurantId);

        return Results.Ok(new
        {
            Email = request.Email,
            Password = DefaultPassword,
            request.RestaurantId,
            request.RestaurantName
        });
    }

    // ========================= 4) DEACTIVATE ADMIN =========================
    private static async Task<IResult> DeactivateAdminsByRestaurant(
        string restaurantId,
        UserManager<ApplicationUser> userMgr)
    {
        if (string.IsNullOrWhiteSpace(restaurantId))
            return Results.BadRequest("RestaurantId is required.");

        var users = await userMgr.Users
            .Where(u => u.RestaurantId == restaurantId && u.UserType == "RestaurantAdmin")
            .ToListAsync();

        if (users.Count == 0)
            return Results.NotFound();

        foreach (var u in users)
        {
            u.IsRestaurantActive = false;
            u.RestaurantStatus = "Closed";

            // Khoá luôn login (phòng trường hợp client vẫn giữ token cũ)
            u.LockoutEnabled = true;
            u.LockoutEnd = DateTimeOffset.MaxValue;

            await userMgr.UpdateAsync(u);

            Log.Information("Deactivated RestaurantAdmin {Email} for Restaurant {RestaurantId}",
                u.Email, restaurantId);
        }

        return Results.Ok(new { message = "Restaurant admin deactivated." });
    }

    // ========================= 5) ACTIVATE ADMIN =========================
    private static async Task<IResult> ActivateAdminsByRestaurant(
        string restaurantId,
        UserManager<ApplicationUser> userMgr)
    {
        if (string.IsNullOrWhiteSpace(restaurantId))
            return Results.BadRequest("RestaurantId is required.");

        var users = await userMgr.Users
            .Where(u => u.RestaurantId == restaurantId && u.UserType == "RestaurantAdmin")
            .ToListAsync();

        if (users.Count == 0)
            return Results.NotFound();

        foreach (var u in users)
        {
            u.IsRestaurantActive = true;
            u.RestaurantStatus = "Active";

            // Mở khoá login
            u.LockoutEnd = null;
            // LockoutEnabled có thể giữ nguyên true để hệ thống còn dùng lockout do login sai password
            // hoặc set false nếu bạn không dùng cơ chế đó.
            // u.LockoutEnabled = false;

            await userMgr.UpdateAsync(u);

            Log.Information("Activated RestaurantAdmin {Email} for Restaurant {RestaurantId}",
                u.Email, restaurantId);
        }

        return Results.Ok(new { message = "Restaurant admin activated." });
    }
}
