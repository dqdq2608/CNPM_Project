using Delivery.Domain.Drone;
using Delivery.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Delivery.API.Apis;

public static class DroneAssignmentsApi
{
    public static IEndpointRouteBuilder MapDroneAssignmentsApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/drone-assignments");

        // GET /api/drone-assignments?orderId=123
        group.MapGet("/", async (int? orderId, DeliveryDbContext db) =>
        {
            IQueryable<DroneAssignment> query = db.DroneAssignments;

            if (orderId.HasValue)
            {
                query = query.Where(x => x.DeliveryOrderId == orderId.Value);
            }

            var list = await query.ToListAsync();
            return Results.Ok(list);
        });

        // GET /api/drone-assignments/{id}
        group.MapGet("/{id:int}", async (int id, DeliveryDbContext db) =>
        {
            var assignment = await db.DroneAssignments.FindAsync(id);
            return assignment is null ? Results.NotFound() : Results.Ok(assignment);
        });

        // POST /api/drone-assignments  (assign drone cho 1 delivery order)
        group.MapPost("/", async (CreateDroneAssignmentRequest request, DeliveryDbContext db) =>
        {
            // Option: kiểm tra drone có tồn tại không
            var drone = await db.Drones.FindAsync(request.DroneId);
            if (drone is null) return Results.BadRequest($"Drone {request.DroneId} not found");

            // TODO: có thể kiểm tra DeliveryOrderId hợp lệ ở đây nếu cần

            var assignment = new DroneAssignment
            {
                DroneId = request.DroneId,
                DeliveryOrderId = request.DeliveryOrderId,
                Status = DroneAssignmentStatus.Assigned,
                AssignedAt = DateTime.UtcNow
            };

            db.DroneAssignments.Add(assignment);
            await db.SaveChangesAsync();

            return Results.Created($"/api/drone-assignments/{assignment.Id}", assignment);
        });

        // PUT /api/drone-assignments/{id}/start
        group.MapPut("/{id:int}/start", async (int id, DeliveryDbContext db) =>
        {
            var assignment = await db.DroneAssignments.FindAsync(id);
            if (assignment is null) return Results.NotFound();

            try
            {
                assignment.Start();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }

            await db.SaveChangesAsync();
            return Results.Ok(assignment);
        });

        // PUT /api/drone-assignments/{id}/complete
        group.MapPut("/{id:int}/complete", async (int id, DeliveryDbContext db) =>
        {
            var assignment = await db.DroneAssignments.FindAsync(id);
            if (assignment is null) return Results.NotFound();

            try
            {
                assignment.Complete();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }

            await db.SaveChangesAsync();
            return Results.Ok(assignment);
        });

        // PUT /api/drone-assignments/{id}/fail
        group.MapPut("/{id:int}/fail", async (int id, FailDroneAssignmentRequest request, DeliveryDbContext db) =>
        {
            var assignment = await db.DroneAssignments.FindAsync(id);
            if (assignment is null) return Results.NotFound();

            try
            {
                assignment.Fail(request.Reason);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }

            await db.SaveChangesAsync();
            return Results.Ok(assignment);
        });

        return app;
    }
}

public record CreateDroneAssignmentRequest(
    int DroneId,
    int DeliveryOrderId
);

public record FailDroneAssignmentRequest(
    string Reason
);
