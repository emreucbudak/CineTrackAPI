namespace CineTrack.Application.Features.Auth.Common;

public static class AuthBloomFilterKeys
{
    public const string RegisteredEmails = "bloom:auth:registered-emails";

    public static string PasswordHistory(Guid userId) =>
        $"bloom:auth:password-history:{userId:N}";
}
