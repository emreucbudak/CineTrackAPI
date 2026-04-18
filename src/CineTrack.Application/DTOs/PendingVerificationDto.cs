namespace CineTrack.Application.DTOs;

public record PendingVerificationDto(
    string TemporaryToken,
    DateTime ExpiresAt,
    string Email = "");
