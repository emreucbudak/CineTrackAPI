namespace CineTrack.Application.Features.Auth.Commands.Register;

internal sealed class RegisterVerificationCacheEntry
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int RemainingAttempts { get; set; }
}
