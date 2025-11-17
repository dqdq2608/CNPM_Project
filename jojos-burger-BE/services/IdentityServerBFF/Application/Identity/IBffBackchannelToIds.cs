using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;

namespace IdentityServerBFF.Application.Identity
{
    public interface IBffBackchannelToIds
    {
        Task<DiscoveryDocumentResponse> GetDiscoveryAsync(
            CancellationToken ct = default);

        Task<TokenResponse> ExchangeCodeForTokenAsync(
            string code,
            string redirectUri,
            CancellationToken ct = default);

        Task<TokenResponse> RefreshTokenAsync(
            string refreshToken,
            CancellationToken ct = default);

        Task<TokenResponse> ClientCredentialsAsync(
            string scope,
            CancellationToken ct = default);

        Task<UserInfoResponse> GetUserInfoAsync(
            string accessToken,
            CancellationToken ct = default);

        Task RevokeRefreshTokenAsync(
            string refreshToken,
            CancellationToken ct = default);
    }
}
