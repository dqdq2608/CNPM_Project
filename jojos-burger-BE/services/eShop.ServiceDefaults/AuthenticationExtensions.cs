using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace eShop.ServiceDefaults;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddDefaultAuthentication(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var config = builder.Configuration;

        var identitySection = config.GetSection("Identity");

        if (!identitySection.Exists())
        {
            Console.WriteLine("[AUTH] ⚠ Không có section Identity → SKIP auth");
            return services;
        }

        string identityUrl = identitySection["Url"]?.TrimEnd('/')
                             ?? throw new InvalidOperationException("Identity:Url missing");
        string audience     = identitySection["Audience"]
                             ?? throw new InvalidOperationException("Identity:Audience missing");

        Console.WriteLine("==========================================");
        Console.WriteLine($"[AUTH] IdentityUrl = {identityUrl}");
        Console.WriteLine($"[AUTH] Audience    = {audience}");
        Console.WriteLine("==========================================");

        // Không map "sub" → tránh đổi claim NameIdentifier
        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

        // ====== TẠO RSA PUBLIC KEY TỪ JWKS (n, e) ======
        // JWKS của bạn:
        var n = "v-p6QyfPvN3SwHyS6aQj7JauiEwAb2sPblzajWcc-kGZ8XrS6K_tZTeOvzkSHkJqRockDg2-pQHPaPvzOxQ81lezwcFWPhjuV4XMus1Cup2SL3ql-Y_DH5GEptmEfaDQdXDdUyvU0AXZR6rjehZ3EcWQqYcyGgg_QhnV8dHo5kVslkXezliBg_iXF714PVNag54Agw8-9Cbce2njLxZWHUDvSqJIgRwVrLHo_OWjupy7lEtG5_UkTlrBtjlmSIKTdd4Y7iqKmrGB0hjtXx_6G5TYvbQA5mFOQHqsJkJyh5K9EBlVMR9JgIShtCWXQdgW7Zfu0nPKsx-RHhyx4E9QzQ";
        var e = "AQAB";
        var kid = "35DB8FB887FE2CF90D9CB1EEECB78777";

        var rsa = RSA.Create();
        rsa.ImportParameters(new RSAParameters
        {
            Modulus  = Base64UrlEncoder.DecodeBytes(n),
            Exponent = Base64UrlEncoder.DecodeBytes(e)
        });

        var rsaKey = new RsaSecurityKey(rsa)
        {
            KeyId = kid
        };

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Không bắt buộc dùng Authority nữa, vì đã có key
                // vẫn có thể để để validate issuer
                options.Authority = identityUrl;
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = identityUrl,
                    ValidateIssuer = true,

                    ValidAudience = audience,
                    ValidateAudience = true,

                    IssuerSigningKey = rsaKey,
                    ValidateIssuerSigningKey = true,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("========== TOKEN INVALID ==========");
                        Console.WriteLine(context.Exception.ToString());
                        Console.ResetColor();
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("========== TOKEN VALIDATED ==========");
                        Console.WriteLine("Token hợp lệ ✔");
                        Console.ResetColor();
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
