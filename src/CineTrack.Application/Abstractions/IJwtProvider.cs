using CineTrack.Domain.Entities;

namespace CineTrack.Application.Abstractions;

public interface IJwtProvider
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
    string GenerateRefreshToken();
}
