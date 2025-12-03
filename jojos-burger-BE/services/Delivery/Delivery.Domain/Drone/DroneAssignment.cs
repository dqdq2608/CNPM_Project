namespace Delivery.Domain.Drone;

public class DroneAssignment
{
    public int Id { get; set; }

    public int DroneId { get; set; }
    public int DeliveryOrderId { get; set; }

    public DroneAssignmentStatus Status { get; set; } = DroneAssignmentStatus.Assigned;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }

    // domain methods (optional, nhưng nên giữ)
    public void Start()
    {
        if (Status != DroneAssignmentStatus.Assigned)
            throw new InvalidOperationException("Assignment must be in 'Assigned' state to start.");

        Status = DroneAssignmentStatus.Flying;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != DroneAssignmentStatus.Flying)
            throw new InvalidOperationException("Assignment must be in 'Flying' state to complete.");

        Status = DroneAssignmentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string reason)
    {
        if (Status == DroneAssignmentStatus.Completed)
            throw new InvalidOperationException("Cannot fail a completed assignment.");

        Status = DroneAssignmentStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTime.UtcNow;
    }
}
