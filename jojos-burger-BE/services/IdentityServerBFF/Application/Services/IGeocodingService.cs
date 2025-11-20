using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerBFF.Application.Services
{
    public interface IGeocodingService
    {
        /// <summary>
        /// Nhận vào địa chỉ full (string) và trả về toạ độ (lat, lon).
        /// </summary>
        Task<(double Lat, double Lon)> GeocodeAsync(
            string fullAddress,
            CancellationToken cancellationToken = default);
    }
}
