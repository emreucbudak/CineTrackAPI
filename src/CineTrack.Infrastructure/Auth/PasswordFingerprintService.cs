using System.Security.Cryptography;
using System.Text;
using CineTrack.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CineTrack.Infrastructure.Auth;

public class PasswordFingerprintService : IPasswordFingerprintService
{
    private readonly byte[] _key;

    public PasswordFingerprintService(IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "Jwt:Secret must be configured to create password bloom fingerprints.");
        }

        _key = Encoding.UTF8.GetBytes(secret);
    }

    public string CreateFingerprint(string password)
    {
        using var hmac = new HMACSHA256(_key);
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = hmac.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
