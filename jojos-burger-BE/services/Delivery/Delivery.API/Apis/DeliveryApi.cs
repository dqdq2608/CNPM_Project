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
            AssignedAt = DateTime.UtcNow
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
    IOrderingClient orderingClient)
    {
        // 1. Lấy delivery theo OrderId (FE làm việc với orderId)
        var delivery = await db.DeliveryOrders
            .FirstOrDefaultAsync(d => d.OrderId == orderId);

        if (delivery is null)
            return Results.NotFound(new { message = "Delivery order not found" });

        // 2. Nếu đã Delivered rồi thì chỉ trả trạng thái hiện tại
        if (delivery.Status == DeliveryStatus.Delivered)
        {
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
                delivery.DroneLon));
        }

        // 3. Tìm assignment & drone cho delivery này
        var assignment = await db.DroneAssignments
            .Where(a => a.DeliveryOrderId == delivery.Id)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync();

        if (assignment is null)
        {
            // Nếu chưa có assignment thì coi như lỗi business – chưa gán drone mà đòi tick
            return Results.BadRequest(new { message = "No drone assignment for this delivery order" });
        }

        var drone = await db.Drones.FindAsync(assignment.DroneId);
        if (drone is null)
        {
            return Results.BadRequest(new { message = "Drone not found for assignment" });
        }

        // 4. Nếu drone không phải đang Delivering thì cũng không di chuyển
        if (drone.Status != DroneStatus.Delivering)
        {
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
                delivery.DroneLon));
        }

        // 5. Tính khoảng cách còn lại từ drone -> customer
        var remainingKm = DistanceInKm(
            drone.CurrentLatitude,
            drone.CurrentLongitude,
            delivery.CustomerLat,
            delivery.CustomerLon);

        // Ngưỡng coi như "đã tới nơi" (5m)
        const double arriveThresholdKm = 0.005;

        if (remainingKm <= arriveThresholdKm)
        {
            // 👉 Drone coi như đã giao xong

            // Cập nhật trạng thái DeliveryOrder
            delivery.Status = DeliveryStatus.Delivered;
            delivery.UpdatedAt = DateTime.UtcNow;

            // Assignment hoàn thành
            assignment.Status = DroneAssignmentStatus.Completed;
            assignment.CompletedAt = DateTime.UtcNow;

            // Drone rảnh lại (hoặc bạn có thể set Charging tuỳ logic)
            drone.Status = DroneStatus.Idle;
            drone.CurrentLatitude = delivery.CustomerLat;
            drone.CurrentLongitude = delivery.CustomerLon;
            drone.LastHeartbeatAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            // Báo Ordering để set OrderStatus = Delivered / Completed
            await orderingClient.MarkOrderDeliveredAsync(orderId);

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
                drone.CurrentLatitude,
                drone.CurrentLongitude));
        }

        // 6. Nếu chưa tới nơi → di chuyển drone thêm một đoạn
        // fraction = mỗi tick đi 25% quãng đường còn lại (sau vài tick sẽ tới)
        const double fraction = 0.25;

        var nextLat = drone.CurrentLatitude +
                      (delivery.CustomerLat - drone.CurrentLatitude) * fraction;

        var nextLon = drone.CurrentLongitude +
                      (delivery.CustomerLon - drone.CurrentLongitude) * fraction;

        drone.CurrentLatitude = nextLat;
        drone.CurrentLongitude = nextLon;
        drone.LastHeartbeatAt = DateTime.UtcNow;

        // Nếu assignment mới chỉ Assigned thì chuyển sang InProgress
        if (assignment.Status == DroneAssignmentStatus.Flying)
        {
            assignment.Status = DroneAssignmentStatus.Flying;
            assignment.StartedAt ??= DateTime.UtcNow;
        }

        // DeliveryOrder vẫn đang trên đường
        delivery.Status = DeliveryStatus.InTransit;
        delivery.UpdatedAt = DateTime.UtcNow;

        // Lưu vị trí drone hiện tại vào delivery
        delivery.DroneLat = nextLat;
        delivery.DroneLon = nextLon;

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

}
