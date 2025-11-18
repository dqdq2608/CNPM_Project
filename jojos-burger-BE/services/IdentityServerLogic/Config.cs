using Duende.IdentityServer.Models;
using System.Collections.Generic;

namespace IdentityServerLogic
{
    public static class Config
    {
        // ===== Identity Resources (OpenID Connect) =====
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

                // Custom user info (JojoBurger)
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

        // ===== API RESOURCES =====
        public static IEnumerable<ApiResource> ApiResources =>
            new ApiResource[]
            {
                // Main API
                new ApiResource("api", "Main API")
                {
                    Scopes = { "api" },
                    UserClaims =
                    {
                        "sub","name","email","role",
                        "user_type","restaurant_id","restaurant_name"
                    }
                },

                // 🔹 Ordering API
                new ApiResource("orders", "Ordering API")
                {
                    Scopes = { "orders" },
                    UserClaims =
                    {
                        "sub","name","email","role",
                        "user_type","restaurant_id","restaurant_name"
                    }
                }
            };

        // ===== API SCOPES =====
        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
            {
                new ApiScope("api", "Main API Scope"),

                // 🔹 Ordering API Scope
                new ApiScope("orders", "Ordering API")
            };

        // ===== CLIENTS =====
        public static IEnumerable<Client> Clients =>
            new Client[]
            {
                // ----- BFF (ROPC login from FE) -----
                new Client
                {
                    ClientId = "bff_ro",
                    ClientName = "BFF (Resource Owner Password Grant)",
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,

                    RequireClientSecret = true,
                    ClientSecrets = { new Secret("super-secret".Sha256()) },

                    // Scopes BFF được phép call
                    AllowedScopes =
                    {
                        "openid",
                        "profile",
                        "email",
                        "roles",
                        "user_info",
                        "offline_access",
                        "api",       // Main API
                        "orders"     // 🔹 CHO BFF GỌI ORDERING.API
                    },

                    AllowOfflineAccess = true,
                    AccessTokenLifetime = 3600,
                    RefreshTokenUsage = TokenUsage.ReUse,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    SlidingRefreshTokenLifetime = 1296000
                },

                // ----- Client Credentials cho Postman / backend test -----
                new Client
                {
                    ClientId = "ordering_client",
                    ClientName = "Ordering Test (client_credentials)",

                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    ClientSecrets =
                    {
                        new Secret("ordering-secret".Sha256())
                    },

                    AllowedScopes = { "orders" } // chỉ truy cập Ordering API
                }
            };
    }
}
