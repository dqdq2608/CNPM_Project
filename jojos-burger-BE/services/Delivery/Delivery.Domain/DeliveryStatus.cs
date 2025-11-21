namespace Delivery.Domain;

public enum DeliveryStatus
{
    Pending = 0,   // mới tạo, chưa bay
    InTransit = 1, // drone đang bay
    Delivered = 2, // đã thả hàng xuống điểm giao
    Failed = 3     // lỗi / huỷ
}
