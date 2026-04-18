using CineTrack.Application.Features.Auth.Common;
using CineTrack.Domain.Entities;

namespace CineTrack.Application.Abstractions;

public interface IJwtProvider
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
    (string Token, DateTime ExpiresAt) GenerateTemporaryToken(
        string email,
        string tokenType,
        Guid? userId = null,
        DateTime? expiresAt = null,
        IReadOnlyDictionary<string, string>? claims = null);
    TemporaryTokenValidationResult ValidateTemporaryToken(string token, string? expectedTokenType = null);
    string GenerateRefreshToken();
}
