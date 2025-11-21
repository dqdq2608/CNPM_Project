using Microsoft.AspNetCore.Mvc;
using PaymentProcessor.Apis;
using Microsoft.Extensions.Logging;

namespace PaymentProcessor.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentLinkCache _cache;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IPaymentLinkCache cache,
            ILogger<PaymentsController> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        // GET /api/payments/{orderId}
        [HttpGet("{orderId:int}")]
        public IActionResult GetPaymentLink(int orderId)
        {
            var url = _cache.Get(orderId);

            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.LogInformation(
                    "[PAYMENT] No cached payment link found for OrderId={OrderId}",
                    orderId);

                return NotFound(new
                {
                    orderId,
                    message = "Payment link not found or not generated yet"
                });
            }

            _logger.LogInformation(
                "[PAYMENT] Returning cached payment link for OrderId={OrderId}: {Url}",
                orderId,
                url);

            return Ok(new
            {
                orderId,
                paymentUrl = url
            });
        }
    }
}
