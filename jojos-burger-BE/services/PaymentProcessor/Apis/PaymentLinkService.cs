using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PaymentProcessor.Apis
{
    public record PaymentLinkResult(
        bool IsSuccess,
        string? PaymentUrl,
        bool IsNewLink,
        string? ErrorCode,
        string? ErrorMessage
    );

    public interface IPaymentLinkService
    {
        Task<PaymentLinkResult> CreateAndCachePaymentLinkAsync(
            int orderId,
            decimal amount,
            string? description,
            string returnUrl,
            string cancelUrl,
            CancellationToken ct = default);
    }

    public class PaymentLinkService : IPaymentLinkService
    {
        private readonly IPaymentApi _paymentApi;
        private readonly IPaymentLinkCache _cache;
        private readonly ILogger<PaymentLinkService> _logger;

        public PaymentLinkService(
            IPaymentApi paymentApi,
            IPaymentLinkCache cache,
            ILogger<PaymentLinkService> logger)
        {
            _paymentApi = paymentApi;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PaymentLinkResult> CreateAndCachePaymentLinkAsync(
            int orderId,
            decimal amount,
            string? description,
            string returnUrl,
            string cancelUrl,
            CancellationToken ct = default)
        {
            // Nếu bạn muốn dùng lại link cũ, có thể bật đoạn này:
            // var existing = _cache.Get(orderId);
            // if (!string.IsNullOrWhiteSpace(existing))
            // {
            //     _logger.LogInformation("[PAYMENT] Reuse cached payment link for OrderId={OrderId}: {Url}", orderId, existing);
            //     return new PaymentLinkResult(true, existing, false, null, null);
            // }

            var request = new CreatePaymentRequest
            {
                OrderId     = orderId,
                Amount      = amount,
                Description = description,
                ReturnUrl   = returnUrl,
                CancelUrl   = cancelUrl
            };

            _logger.LogInformation(
                "[PAYMENT] Creating payment link for OrderId={OrderId}, Amount={Amount}",
                orderId, amount);

            var resp = await _paymentApi.CreatePaymentAsync(request);

            if (!resp.IsSuccess || string.IsNullOrWhiteSpace(resp.PaymentUrl))
            {
                _logger.LogWarning(
                    "[PAYMENT] Failed to create payment link for OrderId={OrderId}. Error={Code} - {Message}",
                    orderId, resp.ErrorCode, resp.ErrorMessage);

                return new PaymentLinkResult(
                    IsSuccess: false,
                    PaymentUrl: null,
                    IsNewLink: false,
                    ErrorCode: resp.ErrorCode,
                    ErrorMessage: resp.ErrorMessage
                );
            }

            // ✅ Lưu vào cache tạm
            _cache.Set(orderId, resp.PaymentUrl!);

            _logger.LogInformation(
                "[PAYMENT] Payment link cached for OrderId={OrderId}: {Url}",
                orderId, resp.PaymentUrl);

            return new PaymentLinkResult(
                IsSuccess: true,
                PaymentUrl: resp.PaymentUrl,
                IsNewLink: true,
                ErrorCode: null,
                ErrorMessage: null
            );
        }
    }
}
