using Delivery.API.Dtos.Requests;
using Delivery.API.Dtos.Responses;
using Delivery.Domain;
using Delivery.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Delivery.API.Clients;
using Delivery.Domain.Drone;

namespace Delivery.API.Apis;

public static class DeliveryApi
{
    public static IEndpointRouteBuilder MapDeliveryApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/deliveries");

        api.MapPost("/", CreateDeliveryAsync);
        api.MapPost("/start/{orderId:int}", StartDeliveryAsync);
        api.MapPost("/{id:int}/release", ReleaseDeliveryAsync);
        api.MapGet("/by-order/{orderId:int}", GetByOrderAsync);
        api.MapPost("/{orderId:int}/tick", TickDeliveryAsync);
        return app;
    }

    static double ToRadians(double angle) => Math.PI * angle / 180.0;

    static double DistanceInKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    static decimal CalcFee(double distanceKm)
    {
        var baseFee = 15000m;
        var perKm = 5000m;
        if (distanceKm <= 1) return baseFee;
        return baseFee + (decimal)(distanceKm - 1) * perKm;
    }

    static async Task<IResult> CreateDeliveryAsync(
        CreateDeliveryRequest req,
        DeliveryDbContext db)
    {
        var distance = DistanceInKm(
            req.RestaurantLat, req.RestaurantLon,
            req.CustomerLat, req.CustomerLon);

        var fee = CalcFee(distance);

        var delivery = new DeliveryOrder
        {
            OrderId = req.OrderId,
            RestaurantLat = req.RestaurantLat,
            RestaurantLon = req.RestaurantLon,
            CustomerLat = req.CustomerLat,
            CustomerLon = req.CustomerLon,
            DistanceKm = distance,
            DeliveryFee = fee,
            Status = DeliveryStatus.Pending,
            DroneLat = null,
            DroneLon = null
        };

        db.DeliveryOrders.Add(delivery);
        await db.SaveChangesAsync();

        return Results.Created($"/api/deliveries/{delivery.Id}",
            new DeliveryResponse(
                delivery.Id,
                delivery.OrderId,
                delivery.RestaurantLat,
                delivery.RestaurantLon,
                delivery.CustomerLat,
                delivery.CustomerLon,
                delivery.DistanceKm,
                delivery.DeliveryFee,
                delivery.Status.ToString(),
                delivery.DroneLat,
                delivery.DroneLon
            ));
    }

    static async Task<IResult> StartDeliveryAsync(
    int orderId,
    StartDeliveryRequest request,
    DeliveryDbContext db)
    {
        // 1. Lấy DeliveryOrder theo OrderId
        var delivery = await db.DeliveryOrders
            .FirstOrDefaultAsync(d => d.OrderId == orderId);

        if (delivery is null)
        {
            return Results.NotFound(new { message = "Delivery order not found" });
        }

        // 2. Lấy Drone theo DroneId từ request
        var drone = await db.Drones
            .FirstOrDefaultAsync(d => d.Id == request.DroneId);

        if (drone is null)
        {
            return Results.BadRequest(new { message = "Drone not found" });
        }

        if (drone.Status != DroneStatus.Idle)
        {
            return Results.BadRequest(new { message = "Drone is not idle" });
        }

        // 3. Tạo DroneAssignment mới
        var assignment = new DroneAssignment
        {
            DroneId = drone.Id,
            DeliveryOrderId = delivery.Id,
            Status = DroneAssignmentStatus.Assigned,
            AssignedAt = DateTime.UtcNow,
            StartedAt = DateTime.UtcNow,
        };

        db.DroneAssignments.Add(assignment);

        // 4. Cập nhật trạng thái Drone + DeliveryOrder
        drone.Status = DroneStatus.Delivering;
        drone.LastHeartbeatAt = DateTime.UtcNow;

        // Lưu vị trí drone vào DeliveryOrder
        delivery.Status = DeliveryStatus.InTransit;
        delivery.UpdatedAt = DateTime.UtcNow;
        delivery.DroneLat = drone.CurrentLatitude;
        delivery.DroneLon = drone.CurrentLongitude;

        await db.SaveChangesAsync();

        // 5. Trả về DeliveryResponse 
        return Results.Ok(
            new DeliveryResponse(
                delivery.Id,
                delivery.OrderId,
                delivery.RestaurantLat,
                delivery.RestaurantLon,
                delivery.CustomerLat,
                delivery.CustomerLon,
                delivery.DistanceKm,
                delivery.DeliveryFee,
                delivery.Status.ToString(),
                delivery.DroneLat,
                delivery.DroneLon
            ));
    }


    static async Task<IResult> ReleaseDeliveryAsync(int id, DeliveryDbContext db)
    {
        var delivery = await db.DeliveryOrders.FindAsync(id);
        if (delivery is null) return Results.NotFound();

        delivery.Status = DeliveryStatus.Delivered;
        delivery.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new DeliveryResponse(
            delivery.Id,
            delivery.OrderId,
            delivery.RestaurantLat,
            delivery.RestaurantLon,
            delivery.CustomerLat,
            delivery.CustomerLon,
            delivery.DistanceKm,
            delivery.DeliveryFee,
            delivery.Status.ToString(),
            delivery.DroneLat,
            delivery.DroneLon
        ));
    }

    static async Task<IResult> GetByOrderAsync(int orderId, DeliveryDbContext db)
    {
        var delivery = await db.DeliveryOrders
            .FirstOrDefaultAsync(d => d.OrderId == orderId);

        if (delivery is null) return Results.NotFound();

        return Results.Ok(new DeliveryResponse(
            delivery.Id,
            delivery.OrderId,
            delivery.RestaurantLat,
            delivery.RestaurantLon,
            delivery.CustomerLat,
            delivery.CustomerLon,
            delivery.DistanceKm,
            delivery.DeliveryFee,
            delivery.Status.ToString(),
            delivery.DroneLat,
            delivery.DroneLon)
        );
    }

    static async Task<IResult> TickDeliveryAsync(
    int orderId,
    DeliveryDbContext db,
    IOrderingClient orderingClient) // có thể bỏ param này nếu không dùng nữa
    {
        // 1. Lấy delivery theo OrderId
        var delivery = await db.DeliveryOrders
            .FirstOrDefaultAsync(d => d.OrderId == orderId);

        if (delivery is null)
            return Results.NotFound(new { message = "Delivery order not found" });

        // 2. Trả về trạng thái hiện tại (không di chuyển, không tính toán gì thêm)
        return Results.Ok(new DeliveryResponse(
            delivery.Id,
            delivery.OrderId,
            delivery.RestaurantLat,
            delivery.RestaurantLon,
            delivery.CustomerLat,
            delivery.CustomerLon,
            delivery.DistanceKm,
            delivery.DeliveryFee,
            delivery.Status.ToString(),
            delivery.DroneLat,
            delivery.DroneLon
        ));
    }
}