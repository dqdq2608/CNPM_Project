using Payment.Providers.Abstractions;

namespace Payment.Providers.Momo;

public class MomoPaymentProvider : IPaymentProvider
{
    private readonly MomoClient _client;

    public MomoPaymentProvider(MomoClient client)
    {
        _client = client;
    }

    public Task<PaymentResult> CreatePaymentAsync(OrderPaymentData order)
        => _client.CreatePaymentAsync(order);
}
