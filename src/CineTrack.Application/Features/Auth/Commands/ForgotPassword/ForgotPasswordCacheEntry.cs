namespace CineTrack.Application.Features.Auth.Commands.ForgotPassword;

internal sealed class ForgotPasswordCacheEntry
{
    public Guid? UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int RemainingAttempts { get; set; }
}
