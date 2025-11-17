using Duende.IdentityServer.Models;
using System.Collections.Generic;

namespace IdentityServerLogic
{
    public static class Config
    {
        // Các IdentityResource (OpenID Connect)
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),

                // Roles (role claim)
                new IdentityResource(
                    name: "roles",
                    displayName: "Roles",
                    userClaims: new[] { "role" }
                ),

                // Custom user info cho hệ thống JojoBurger
                new IdentityResource(
                    name: "user_info",
                    displayName: "Custom user info",
                    userClaims: new[]
                    {
                        "user_type",
                        "restaurant_id",
                        "restaurant_name"
                    })
            };

        // API Resource chính
        public static IEnumerable<ApiResource> ApiResources =>
            new ApiResource[]
            {
                new ApiResource("api", "Main API")
                {
                    Scopes = { "api" },

                    UserClaims =
                    {
                        "sub",
                        "name",
                        "email",
                        "role",
                        "user_type",
                        "restaurant_id",
                        "restaurant_name"     
                    }
                }
            };

        // API Scope
        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                new ApiScope("api", "Main API Scope"),
                new ApiScope("scope1"),
                new ApiScope("scope2")
            };

        // Client cho BFF (ROPC)
        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                new Client
                {
                    ClientId = "bff_ro",
                    ClientName = "BFF (Resource Owner Password Grant)",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

                    RequireClientSecret = true,
                    ClientSecrets = { new Secret("super-secret".Sha256()) },

                    // Scopes BFF cần
                    AllowedScopes =
                    {
                        "openid",
                        "profile",
                        "email",
                        "roles",
                        "user_info",
                        "offline_access",
                        "api"
                    },

                    // Refresh token
                    AllowOfflineAccess = true,
                    AccessTokenLifetime = 3600,
                    RefreshTokenUsage = TokenUsage.ReUse,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    SlidingRefreshTokenLifetime = 1296000 // 15 ngày
                }
            };
    }
}
