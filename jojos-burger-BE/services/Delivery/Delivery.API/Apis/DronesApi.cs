using Delivery.Domain.Drone;
using Delivery.Domain;
using Delivery.Infrastructure;
using Delivery.API.Clients;
using Microsoft.EntityFrameworkCore;

namespace Delivery.API.Apis;

public static class DronesApi
{
    public static IEndpointRouteBuilder MapDronesApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/drones");

        // GET /api/drones?restaurantId={restaurantId}
        group.MapGet("/", async (Guid? restaurantId, DeliveryDbContext db) =>
        {
            IQueryable<Drone> query = db.Drones;

            if (restaurantId.HasValue)
            {
                query = query.Where(d => d.RestaurantId == restaurantId.Value);
            }

            var drones = await query.ToListAsync();

            var result = new List<DroneDto>(drones.Count);

            foreach (var d in drones)
            {
                var needsTick = await ComputeNeedsTickAsync(d, db);

                result.Add(new DroneDto(
                    d.Id,
                    d.Code,
                    (int)d.Status,
                    d.CurrentLatitude,
                    d.CurrentLongitude,
                    d.RestaurantId,
                    needsTick
                ));
            }

            return Results.Ok(result);
        });

        // POST /api/drones
        group.MapPost("/", async (CreateDroneRequest request, DeliveryDbContext db) =>
        {
            var drone = new Drone
            {
                Code = request.Code,
                RestaurantId = request.RestaurantId,
                Status = DroneStatus.Idle,
                CurrentLatitude = request.InitialLatitude,
                CurrentLongitude = request.InitialLongitude,
                LastHeartbeatAt = DateTime.UtcNow
            };

            db.Drones.Add(drone);
            await db.SaveChangesAsync();

            return Results.Created($"/api/drones/{drone.Id}", drone);
        });

        // POST /api/drones/{id}/tick
        group.MapPost("/{id:int}/tick", async (int id, DeliveryDbContext db, IOrderingClient orderingClient) =>
        {
            return await TickDroneAsync(id, db, orderingClient);
        });

        // PUT /api/drones/{id}/status
        group.MapPut("/{id:int}/status", async (
            int id,
            UpdateDroneStatusRequest request,
            DeliveryDbContext db
        ) =>
        {
            var drone = await db.Drones.FindAsync(id);
            if (drone is null)
            {
                return Results.NotFound(new { message = "Drone not found" });
            }

            // Ép từ int sang enum
            drone.Status = (DroneStatus)request.Status;
            drone.LastHeartbeatAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(drone);
        });

        // PUT /api/drones/{id}/location
        group.MapPut("/{id:int}/location", async (int id, UpdateDroneLocationRequest request, DeliveryDbContext db) =>
        {
            var drone = await db.Drones.FindAsync(id);
            if (drone is null) return Results.NotFound();

            drone.CurrentLatitude = request.Latitude;
            drone.CurrentLongitude = request.Longitude;
            drone.LastHeartbeatAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(drone);
        });

        return app;
    }

    private static async Task<IResult> TickDroneAsync(int droneId, DeliveryDbContext db, IOrderingClient orderingClient)
    {
        // 1. Tìm drone
        var drone = await db.Drones.FindAsync(droneId);
        if (drone is null)
            return Results.NotFound();

        // 🚫 Nếu drone đang Offline hoặc Maintenance => KHÔNG tick, không override gì
        if (drone.Status == DroneStatus.Offline ||
            drone.Status == DroneStatus.Maintenance)
        {
            return Results.NoContent();
        }

        // 2. Tìm assignment mới nhất của drone
        var assignment = await db.DroneAssignments
            .Where(a => a.DroneId == droneId)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync();

        if (assignment is null)
            return Results.NoContent();

        // 3. Tìm DeliveryOrder
        var delivery = await db.DeliveryOrders.FindAsync(assignment.DeliveryOrderId);
        if (delivery is null)
            return Results.NoContent();

        //
        // ==========================================================
        // PHASE 1 — BAY TỚI KHÁCH (InTransit)
        // ==========================================================
        //
        if (delivery.Status == DeliveryStatus.InTransit)
        {
            var remainingKm = DistanceInKm(
                drone.CurrentLatitude,
                drone.CurrentLongitude,
                delivery.CustomerLat,
                delivery.CustomerLon);

            const double thresholdKm = 0.05; // 50m

            if (remainingKm <= thresholdKm)
            {
                // ĐÃ TỚI NƠI
                drone.CurrentLatitude = delivery.CustomerLat;
                drone.CurrentLongitude = delivery.CustomerLon;
                drone.Status = DroneStatus.Idle;
                drone.LastHeartbeatAt = DateTime.UtcNow;

                assignment.Status = DroneAssignmentStatus.Completed;
                assignment.CompletedAt = DateTime.UtcNow;

                delivery.Status = DeliveryStatus.Delivered;
                delivery.DroneLat = drone.CurrentLatitude;
                delivery.DroneLon = drone.CurrentLongitude;
                delivery.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                await orderingClient.MarkOrderDeliveredAsync(delivery.OrderId);
                return Results.NoContent();
            }

            // Move 25% distance
            const double fraction = 0.25;
            var nextLat = drone.CurrentLatitude + (delivery.CustomerLat - drone.CurrentLatitude) * fraction;
            var nextLon = drone.CurrentLongitude + (delivery.CustomerLon - drone.CurrentLongitude) * fraction;

            drone.CurrentLatitude = nextLat;
            drone.CurrentLongitude = nextLon;
            drone.LastHeartbeatAt = DateTime.UtcNow;

            delivery.DroneLat = nextLat;
            delivery.DroneLon = nextLon;
            delivery.UpdatedAt = DateTime.UtcNow;

            assignment.StartedAt ??= DateTime.UtcNow;
            assignment.Status = DroneAssignmentStatus.Flying;

            await db.SaveChangesAsync();
            return Results.NoContent();
        }

        //
        // ==========================================================
        // PHASE 2 — BAY NGƯỢC VỀ NHÀ HÀNG (Delivered)
        // ==========================================================
        //
        if (delivery.Status == DeliveryStatus.Delivered)
        {
            var remainingKm = DistanceInKm(
                drone.CurrentLatitude,
                drone.CurrentLongitude,
                delivery.RestaurantLat,
                delivery.RestaurantLon);

            const double backThresholdKm = 0.05;

            if (remainingKm <= backThresholdKm)
            {
                drone.CurrentLatitude = delivery.RestaurantLat;
                drone.CurrentLongitude = delivery.RestaurantLon;
                drone.Status = DroneStatus.Idle;
                drone.LastHeartbeatAt = DateTime.UtcNow;

                delivery.DroneLat = drone.CurrentLatitude;
                delivery.DroneLon = drone.CurrentLongitude;
                delivery.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                return Results.NoContent();
            }

            const double fraction = 0.25;

            var nextLat = drone.CurrentLatitude + (delivery.RestaurantLat - drone.CurrentLatitude) * fraction;
            var nextLon = drone.CurrentLongitude + (delivery.RestaurantLon - drone.CurrentLongitude) * fraction;

            drone.CurrentLatitude = nextLat;
            drone.CurrentLongitude = nextLon;
            drone.LastHeartbeatAt = DateTime.UtcNow;

            delivery.DroneLat = nextLat;
            delivery.DroneLon = nextLon;
            delivery.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        }

        return Results.NoContent();
    }

    private static async Task<bool> ComputeNeedsTickAsync(Drone drone, DeliveryDbContext db)
    {
        // 1. Nếu drone đang Offline / Maintenance => KHÔNG tick
        if (drone.Status == DroneStatus.Offline ||
            drone.Status == DroneStatus.Maintenance)
        {
            return false;
        }

        // 2. Tìm assignment mới nhất
        var assignment = await db.DroneAssignments
            .Where(a => a.DroneId == drone.Id)
            .OrderByDescending(a => a.AssignedAt)
            .FirstOrDefaultAsync();

        if (assignment is null)
        {
            // chưa từng / không còn nhiệm vụ
            return false;
        }

        // 3. Tìm delivery tương ứng
        var delivery = await db.DeliveryOrders.FindAsync(assignment.DeliveryOrderId);
        if (delivery is null)
        {
            return false;
        }

        // 4. Nếu delivery đang InTransit => chắc chắn cần tick (bay tới khách)
        if (delivery.Status == DeliveryStatus.InTransit)
        {
            return true;
        }

        // 5. Nếu delivery đã Delivered => chỉ tick nếu drone CHƯA về đến nhà hàng
        if (delivery.Status == DeliveryStatus.Delivered)
        {
            var remainingKm = DistanceInKm(
                drone.CurrentLatitude,
                drone.CurrentLongitude,
                delivery.RestaurantLat,
                delivery.RestaurantLon);

            const double backThresholdKm = 0.05; // ~50m

            return remainingKm > backThresholdKm;
        }

        // Các trạng thái khác (Created, Canceled, v.v.) => không cần tick
        return false;
    }

    // Helper Haversine
    private static double DistanceInKm(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371; // km
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);
        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double deg) => deg * (Math.PI / 180.0);
}

public record DroneDto(
    int Id,
    string Code,
    int Status,
    double? CurrentLatitude,
    double? CurrentLongitude,
    Guid RestaurantId,
    bool NeedsTick
);

public record CreateDroneRequest(
    string Code,
    Guid RestaurantId,
    double InitialLatitude,
    double InitialLongitude
);

public record UpdateDroneStatusRequest(DroneStatus Status);

public record UpdateDroneLocationRequest(
    double Latitude,
    double Longitude
);

