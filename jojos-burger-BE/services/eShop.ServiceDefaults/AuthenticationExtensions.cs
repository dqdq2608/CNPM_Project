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
        string audience = identitySection["Audience"]
                          ?? throw new InvalidOperationException("Identity:Audience missing");

        Console.WriteLine("==========================================");
        Console.WriteLine($"[AUTH] IdentityUrl = {identityUrl}");
        Console.WriteLine($"[AUTH] Audience    = {audience}");
        Console.WriteLine("==========================================");

        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

        // 🔑 LẤY TỪ JWKS MỚI CỦA IDS
        var n   = "uOP8LRKwMg6SpI8bK11Oi8XyBIc61qUsbJ-hOpFUfTsoqJelGF9XlFEyJbMc-9uUsLZcC_vL4Cj_KtkkpVP5CXjZtEY54ovqeAL8sg6xs423ydkU6JvcmJO9gexrfYwHXkYP9wIFtPMT0sayoh3PZ-GN-oFZStaektpXnE8TRA7BnZOjxd-kVCzugiBsYI6I78sbO5Zdz3oAiGH9yPYYNxSIFGtMnKsdDNCDjbvI_8WGARd8Au2W4eg03U5IILRoDLeuoCpkWnKouEK7GtvN5GRnibFXeuk-_yX3mTuMZre3gpjZ7x9mflE3FEsitiL9P2IT33rQSIzEKO7t8BcW1Q";
        var e   = "AQAB";   // thường là "AQAB"
        var kid = "01C12CC2C6AAD9BBEC72834227B7B2F1>"; // ví dụ "01C12CC2C6AAD9BBEC72834227B7B2F1"

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
