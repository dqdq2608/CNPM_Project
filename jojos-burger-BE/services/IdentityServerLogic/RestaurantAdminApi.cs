using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;          // ⭐ để dùng ToListAsync
using Serilog;
using IdentityServerLogic.Identity;          // ApplicationUser

namespace IdentityServerLogic;

public static class RestaurantAdminApi
{
    private const string DefaultPassword = "123456";

    public static IEndpointRouteBuilder MapRestaurantAdminApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/restaurant-admins");

        // ============================
        // 1) Tạo admin mới
        // ============================
        api.MapPost("/", CreateRestaurantAdmin);

        // ============================
        // 2) Lấy admin theo RestaurantId (cho Catalog hiển thị email)
        //    GET /api/restaurant-admins/by-restaurant/{restaurantId}
        // ============================
        api.MapGet("/by-restaurant/{restaurantId}", GetAdminsByRestaurant);

        // ============================
        // 3) Xoá admin theo RestaurantId (Catalog gọi khi xoá nhà hàng)
        //    DELETE /api/restaurant-admins/by-restaurant/{restaurantId}
        // ============================
        api.MapDelete("/by-restaurant/{restaurantId}", DeleteRestaurantAdminsByRestaurant);


        return app;
    }

    // ============================
    // ⭐ 2) LẤY ADMIN THEO RESTAURANT
    // ============================
    private static async Task<IResult> GetAdminsByRestaurant(
        string restaurantId,
        UserManager<ApplicationUser> userMgr)
    {
        if (string.IsNullOrWhiteSpace(restaurantId))
            return Results.BadRequest("RestaurantId is required.");

        // RestaurantId trong ApplicationUser đang lưu kiểu string
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

        // Có thể trả list (phòng sau này 1 nhà hàng nhiều admin),
        // Catalog chỉ cần lấy user đầu tiên để hiển thị email.
        return Results.Ok(users);
    }

    // ============================
    // ⭐ 3) XOÁ ADMIN THEO RESTAURANT
    // ============================
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

            Log.Information("Deleted RestaurantAdmin {Email} for Restaurant {RestaurantId}", user.Email, restaurantId);
        }

        return Results.NoContent();
    }
    // ============================
    // ⭐ 1) TẠO TÀI KHOẢN ADMIN
    // ============================
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
            // 💡 Lưu RestaurantId dạng string (Catalog gửi Guid.ToString())
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
