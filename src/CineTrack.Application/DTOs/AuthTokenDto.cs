namespace CineTrack.Application.DTOs;

public record AuthTokenDto(
    string Token,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
