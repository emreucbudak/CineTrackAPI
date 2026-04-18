namespace CineTrack.Application.Features.Auth.Common;

public sealed record TemporaryTokenPayload(
    string TokenType,
    string Email,
    Guid? UserId = null,
    string? TokenId = null,
    IReadOnlyDictionary<string, string>? Claims = null);
