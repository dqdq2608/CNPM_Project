using Duende.IdentityServer.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace IdentityServerLogic
{
    public static class Config
    {
        // Cấp các IdentityResource chuẩn (OpenId, Profile, Email)
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResource("roles", new[] {"role"})
            };

        // Thêm ApiResource chính để access_token chứa đủ claim
        public static IEnumerable<ApiResource> ApiResources =>
            new[]
            {
                new ApiResource("api", "Main API")
                {
                    Scopes     = { "api" },
                    UserClaims = { "sub", "name", "preferred_username", "email", "given_name", "family_name", "role" }
                }
            };

        // ApiScope – giữ nguyên hoặc thêm scope "api"
        public static IEnumerable<ApiScope> ApiScopes =>
            new[]
            {
                new ApiScope("scope1"),
                new ApiScope("scope2"),
                new ApiScope("api", "Main API"),
            };

        // Client cho BFF (ROPC)
        public static IEnumerable<Client> Clients =>
            new[]
            {
                new Client
                {
                    ClientId = "bff_ro",
                    ClientName = "BFF (ROPC)",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                    RequireClientSecret = true,
                    ClientSecrets = { new Secret("super-secret".Sha256()) },

                    // Quan trọng: phải có các scope sau
                    AllowedScopes =
                    {
                        "openid",
                        "profile",
                        "email",
                        "roles",
                        "offline_access",
                        "api"
                    },

                    AllowOfflineAccess = true,
                    AccessTokenLifetime = 3600,
                    RefreshTokenUsage = TokenUsage.ReUse,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    SlidingRefreshTokenLifetime = 1296000
                },
            };
    }
}
