using System.Collections.Concurrent;

namespace PaymentProcessor.Apis
{
    /// <summary>
    /// Cache tạm trong RAM để giữ paymentUrl theo OrderId.
    /// </summary>
    public interface IPaymentLinkCache
    {
        void Set(int orderId, string paymentUrl);
        string? Get(int orderId);
        void Remove(int orderId);
    }

    public class InMemoryPaymentLinkCache : IPaymentLinkCache
    {
        private readonly ConcurrentDictionary<int, string> _cache = new();

        public void Set(int orderId, string paymentUrl)
        {
            _cache[orderId] = paymentUrl;
        }

        public string? Get(int orderId)
        {
            return _cache.TryGetValue(orderId, out var url) ? url : null;
        }

        public void Remove(int orderId)
        {
            _cache.TryRemove(orderId, out _);
        }
    }
}
