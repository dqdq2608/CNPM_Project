using System.Text.Json.Serialization;

namespace eShop.Ordering.Domain.AggregatesModel.OrderAggregate;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Submitted = 1,
    AwaitingValidation = 2,
    StockConfirmed = 3,
    Paid = 4,
    Preparing = 5,
    ReadyForDelivery = 6,
    Delivering = 7,  // drone đang bay
    Delivered = 8,   // hệ thống (drone) nói đã giao xong
    Completed = 9,   // khách xác nhận đã nhận
    Cancelled = 10
}
