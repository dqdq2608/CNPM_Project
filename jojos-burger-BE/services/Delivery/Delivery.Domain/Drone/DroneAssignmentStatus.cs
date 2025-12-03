namespace Delivery.Domain.Drone;

public enum DroneAssignmentStatus
{
    Assigned = 0,   // đã gán drone cho đơn nhưng chưa cất cánh
    Flying = 1,     // đang bay, đang giao
    Completed = 2,  // giao xong
    Failed = 3      // thất bại (hết pin, lỗi, thời tiết, v.v.)
}
