using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Hosting;

namespace IIoT.Infrastructure.Authentication;

public static class JwtSecretResolver
{
    public static string Resolve(IHostEnvironment environment, string? configuredSecret)
    {
        if (!string.IsNullOrWhiteSpace(configuredSecret))
        {
            return configuredSecret;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "JwtSettings:Secret is missing. Configure it via user-secrets or environment variables.");
        }

        var seed = $"{environment.ApplicationName}:development-jwt";
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(seed)));
    }
}
