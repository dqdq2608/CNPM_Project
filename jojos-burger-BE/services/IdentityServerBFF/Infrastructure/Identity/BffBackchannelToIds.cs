using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;
using IdentityServerBFF.Application.Identity;
using Microsoft.Extensions.Configuration;

namespace IdentityServerBFF.Infrastructure.Identity
{
    /// Implementation thật dùng HttpClient + Duende.IdentityModel
    /// để gọi IdentityServer qua backchannel.
    public class BffBackchannelToIds : IBffBackchannelToIds
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public BffBackchannelToIds(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        private string Authority => _config["IdentityServer:Authority"]
            ?? throw new InvalidOperationException("IdentityServer:Authority is not configured");

        private string ClientId => _config["IdentityServer:ClientId"]
            ?? throw new InvalidOperationException("IdentityServer:ClientId is not configured");

        private string ClientSecret => _config["IdentityServer:ClientSecret"]
            ?? throw new InvalidOperationException("IdentityServer:ClientSecret is not configured");

        public async Task<DiscoveryDocumentResponse> GetDiscoveryAsync(CancellationToken ct = default)
        {
            var disco = await _httpClient.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = Authority,
                Policy =
                {
                    RequireHttps = Authority.StartsWith("https", StringComparison.OrdinalIgnoreCase)
                }
            }, ct);

            if (disco.IsError)
            {
                throw new InvalidOperationException($"IDS discovery error: {disco.Error}");
            }

            return disco;
        }

        public async Task<TokenResponse> ExchangeCodeForTokenAsync(
            string code,
            string redirectUri,
            CancellationToken ct = default)
        {
            var disco = await GetDiscoveryAsync(ct);

            var response = await _httpClient.RequestAuthorizationCodeTokenAsync(
                new AuthorizationCodeTokenRequest
                {
                    Address = disco.TokenEndpoint,
                    ClientId = ClientId,
                    ClientSecret = ClientSecret,
                    Code = code,
                    RedirectUri = redirectUri
                }, ct);

            if (response.IsError)
            {
                throw new InvalidOperationException($"Token error: {response.Error}");
            }

            return response;
        }

        public async Task<TokenResponse> RefreshTokenAsync(
            string refreshToken,
            CancellationToken ct = default)
        {
            var disco = await GetDiscoveryAsync(ct);

            var response = await _httpClient.RequestRefreshTokenAsync(
                new RefreshTokenRequest
                {
                    Address = disco.TokenEndpoint,
                    ClientId = ClientId,
                    ClientSecret = ClientSecret,
                    RefreshToken = refreshToken
                }, ct);

            if (response.IsError)
            {
                throw new InvalidOperationException($"Refresh token error: {response.Error}");
            }

            return response;
        }

        public async Task<TokenResponse> ClientCredentialsAsync(
            string scope,
            CancellationToken ct = default)
        {
            var disco = await GetDiscoveryAsync(ct);

            var response = await _httpClient.RequestClientCredentialsTokenAsync(
                new ClientCredentialsTokenRequest
                {
                    Address = disco.TokenEndpoint,
                    ClientId = ClientId,
                    ClientSecret = ClientSecret,
                    Scope = scope
                }, ct);

            if (response.IsError)
            {
                throw new InvalidOperationException($"Client credentials error: {response.Error}");
            }

            return response;
        }

        public async Task<UserInfoResponse> GetUserInfoAsync(
            string accessToken,
            CancellationToken ct = default)
        {
            var disco = await GetDiscoveryAsync(ct);

            var response = await _httpClient.GetUserInfoAsync(new UserInfoRequest
            {
                Address = disco.UserInfoEndpoint,
                Token = accessToken
            }, ct);

            if (response.IsError)
            {
                throw new InvalidOperationException($"UserInfo error: {response.Error}");
            }

            return response;
        }

        public async Task RevokeRefreshTokenAsync(
            string refreshToken,
            CancellationToken ct = default)
        {
            var disco = await GetDiscoveryAsync(ct);

            var response = await _httpClient.RevokeTokenAsync(new TokenRevocationRequest
            {
                Address = disco.RevocationEndpoint,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                Token = refreshToken,
                TokenTypeHint = "refresh_token"
            }, ct);

            if (response.IsError)
            {
                throw new InvalidOperationException($"Revoke token error: {response.Error}");
            }
        }
    }
}
