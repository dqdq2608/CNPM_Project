using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityServerBFF.Application.Services;

namespace IdentityServerBFF.Infrastructure.Services
{
    /// <summary>
    /// Geocoding fake để demo: map một số địa chỉ "quen thuộc" sang toạ độ cố định.
    /// Những địa chỉ khác sẽ trả về vị trí mặc định (ví dụ trung tâm TP.HCM).
    /// </summary>
    public class FakeGeocodingService : IGeocodingService
    {
        public Task<(double Lat, double Lon)> GeocodeAsync(
            string fullAddress,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fullAddress))
            {
                // fallback: trung tâm HCM
                return Task.FromResult((10.776889, 106.700806));
            }

            var addr = fullAddress.Trim();

            // Ví dụ 1: phố đi bộ Nguyễn Huệ
            if (addr.Contains("Nguyễn Huệ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult((10.772345, 106.703532));
            }

            // Ví dụ 2: đường Lê Lợi
            if (addr.Contains("Lê Lợi", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult((10.772100, 106.699000));
            }

            // Ví dụ 3: Q.7
            if (addr.Contains("Quận 7", StringComparison.OrdinalIgnoreCase) ||
                addr.Contains("Q7", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult((10.735000, 106.721000));
            }

            // Mặc định: trung tâm HCM (fallback)
            return Task.FromResult((10.776889, 106.700806));
        }
    }
}
