using Delivery.Domain.Drone;
using Delivery.Infrastructure;
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
            return Results.Ok(drones);
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

        // PUT /api/drones/{id}/status
        group.MapPut("/{id:int}/status", async (int id, UpdateDroneStatusRequest request, DeliveryDbContext db) =>
        {
            var drone = await db.Drones.FindAsync(id);
            if (drone is null) return Results.NotFound();

            drone.Status = request.Status;
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
}

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
