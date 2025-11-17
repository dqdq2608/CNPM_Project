namespace Payment.Providers.Abstractions;

public interface IPaymentProvider
{
    /// <summary>
    /// Tạo yêu cầu thanh toán.
    /// </summary>
    /// <param name="order">Thông tin order để tạo thanh toán</param>
    /// <returns>Kết quả thanh toán (paymentUrl nếu trả về MoMo)</returns>
    Task<PaymentResult> CreatePaymentAsync(OrderPaymentData order);
}
