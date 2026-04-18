using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CineTrack.Application.Abstractions;
using CineTrack.Application.Features.Auth.Common;
using CineTrack.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CineTrack.Infrastructure.Auth;

public class JwtProvider : IJwtProvider
{
    private const double DefaultAccessTokenExpirationInMinutes = 30;
    private const double DefaultTemporaryTokenExpirationInMinutes = 10;
    private static readonly HashSet<string> ReservedClaimNames = new(StringComparer.Ordinal)
    {
        AuthTokenClaimNames.TokenType,
        AuthTokenClaimNames.UserId,
        AuthTokenClaimNames.Email,
        AuthTokenClaimNames.TokenId,
        JwtRegisteredClaimNames.Exp,
        JwtRegisteredClaimNames.Nbf,
        JwtRegisteredClaimNames.Iat,
        JwtRegisteredClaimNames.Iss,
        JwtRegisteredClaimNames.Aud
    };

    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _tokenHandler = new()
    {
        MapInboundClaims = false
    };

    public JwtProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var claims = new Claim[]
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("username", user.Username),
            new(AuthTokenClaimNames.TokenType, AuthTokenTypes.Access),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return CreateSignedToken(claims, GetConfiguredLifetime("Jwt:ExpirationInMinutes", DefaultAccessTokenExpirationInMinutes));
    }

    public (string Token, DateTime ExpiresAt) GenerateTemporaryToken(
        string email,
        string tokenType,
        Guid? userId = null,
        DateTime? expiresAt = null,
        IReadOnlyDictionary<string, string>? claims = null)
    {
        var payload = new TemporaryTokenPayload(tokenType, email, userId, null, claims);
        var effectiveExpiresAt = expiresAt.HasValue
            ? NormalizeToUtc(expiresAt.Value)
            : DateTime.UtcNow.Add(GetConfiguredLifetime("Jwt:TemporaryExpirationInMinutes", DefaultTemporaryTokenExpirationInMinutes));

        return CreateSignedToken(CreateTemporaryTokenClaims(payload), effectiveExpiresAt);
    }

    public TemporaryTokenValidationResult ValidateTemporaryToken(string token, string? expectedTokenType = null)
    {
        if (string.IsNullOrWhiteSpace(token))
            return InvalidTemporaryToken();

        try
        {
            var principal = _tokenHandler.ValidateToken(token, CreateTokenValidationParameters(validateLifetime: true), out var validatedToken);

            var tokenType = principal.FindFirst(AuthTokenClaimNames.TokenType)?.Value;
            if (string.IsNullOrWhiteSpace(tokenType) || string.Equals(tokenType, AuthTokenTypes.Access, StringComparison.Ordinal))
                return InvalidTemporaryToken();

            if (!string.IsNullOrWhiteSpace(expectedTokenType) &&
                !string.Equals(tokenType, expectedTokenType, StringComparison.Ordinal))
            {
                return InvalidTemporaryToken();
            }

            var email = principal.FindFirst(AuthTokenClaimNames.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
                return InvalidTemporaryToken();

            Guid? userId = null;
            var userIdClaim = principal.FindFirst(AuthTokenClaimNames.UserId)?.Value;
            if (!string.IsNullOrWhiteSpace(userIdClaim))
            {
                if (!Guid.TryParse(userIdClaim, out var parsedUserId))
                    return InvalidTemporaryToken();

                userId = parsedUserId;
            }

            var additionalClaims = principal.Claims
                .Where(claim => !ReservedClaimNames.Contains(claim.Type))
                .GroupBy(claim => claim.Type, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.Ordinal);

            var payload = new TemporaryTokenPayload(
                tokenType,
                email,
                userId,
                principal.FindFirst(AuthTokenClaimNames.TokenId)?.Value,
                additionalClaims.Count == 0 ? null : additionalClaims);

            var expiresAt = validatedToken is JwtSecurityToken jwtSecurityToken
                ? jwtSecurityToken.ValidTo
                : (DateTime?)null;

            return new TemporaryTokenValidationResult(true, payload, expiresAt);
        }
        catch (SecurityTokenException)
        {
            return InvalidTemporaryToken();
        }
        catch (ArgumentException)
        {
            return InvalidTemporaryToken();
        }
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private (string Token, DateTime ExpiresAt) CreateSignedToken(IEnumerable<Claim> claims, TimeSpan lifetime)
    {
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.Add(lifetime);
        return CreateSignedToken(claims, expiresAt, issuedAt);
    }

    private (string Token, DateTime ExpiresAt) CreateSignedToken(IEnumerable<Claim> claims, DateTime expiresAt, DateTime? issuedAt = null)
    {
        var normalizedIssuedAt = issuedAt.HasValue ? NormalizeToUtc(issuedAt.Value) : DateTime.UtcNow;
        var normalizedExpiresAt = NormalizeToUtc(expiresAt);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            notBefore: normalizedIssuedAt,
            expires: normalizedExpiresAt,
            signingCredentials: GetSigningCredentials());

        return (_tokenHandler.WriteToken(token), normalizedExpiresAt);
    }

    private SigningCredentials GetSigningCredentials()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));
        return new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    private TokenValidationParameters CreateTokenValidationParameters(bool validateLifetime)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = validateLifetime,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!)),
            ClockSkew = TimeSpan.Zero
        };
    }

    private TimeSpan GetConfiguredLifetime(string configurationKey, double fallbackMinutes)
    {
        var configuredValue = _configuration[configurationKey];

        return double.TryParse(configuredValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var minutes) && minutes > 0
            ? TimeSpan.FromMinutes(minutes)
            : TimeSpan.FromMinutes(fallbackMinutes);
    }

    private static IEnumerable<Claim> CreateTemporaryTokenClaims(TemporaryTokenPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.TokenType))
            throw new ArgumentException("Temporary token type is required.", nameof(payload));

        if (string.Equals(payload.TokenType, AuthTokenTypes.Access, StringComparison.Ordinal))
            throw new ArgumentException("Temporary token type cannot be access.", nameof(payload));

        if (string.IsNullOrWhiteSpace(payload.Email))
            throw new ArgumentException("Temporary token email is required.", nameof(payload));

        var tokenId = string.IsNullOrWhiteSpace(payload.TokenId)
            ? Guid.NewGuid().ToString()
            : payload.TokenId;

        var claims = new List<Claim>
        {
            new(AuthTokenClaimNames.TokenType, payload.TokenType),
            new(AuthTokenClaimNames.Email, payload.Email),
            new(AuthTokenClaimNames.TokenId, tokenId)
        };

        if (payload.UserId.HasValue)
            claims.Add(new Claim(AuthTokenClaimNames.UserId, payload.UserId.Value.ToString()));

        if (payload.Claims is not null)
        {
            foreach (var claim in payload.Claims)
            {
                if (string.IsNullOrWhiteSpace(claim.Key) || ReservedClaimNames.Contains(claim.Key))
                    continue;

                claims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        return claims;
    }

    private static TemporaryTokenValidationResult InvalidTemporaryToken() => new(false);

    private static DateTime NormalizeToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
