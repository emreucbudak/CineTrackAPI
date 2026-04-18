namespace CineTrack.Application.Features.Auth.Common;

public sealed record TemporaryTokenValidationResult(
    bool IsValid,
    TemporaryTokenPayload? Payload = null,
    DateTime? ExpiresAt = null);
