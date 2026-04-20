using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Application.Features.Auth.Common;
using CineTrack.Domain.Entities;

namespace CineTrack.Application.Features.Auth.Commands.VerifyLogin;

internal sealed record PendingLoginVerificationCacheItem(
    Guid UserId,
    string Email,
    string Code,
    DateTime ExpiresAt);

internal static class LoginVerificationSupport
{
    internal static readonly TimeSpan VerificationCodeLifetime = TimeSpan.FromMinutes(5);

    internal static string BuildVerificationCacheKey(string temporaryToken) =>
        $"auth:login:verification:{temporaryToken}";

    internal static string BuildLatestChallengeCacheKey(string email) =>
        $"auth:login:latest:{NormalizeEmail(email)}";

    internal static string GenerateVerificationCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6", CultureInfo.InvariantCulture);

    internal static string BuildVerificationEmailHtml(string code)
    {
        var expiresInMinutes = (int)VerificationCodeLifetime.TotalMinutes;

        return $"""
                <html>
                  <body style="margin:0; padding:24px; background:#0f0f14; font-family:Arial, sans-serif; color:#f5f5f5;">
                    <div style="max-width:560px; margin:0 auto; background:#171821; border:1px solid #2b2d3a; border-radius:18px; overflow:hidden;">
                      <div style="padding:24px 24px 8px;">
                        <div style="display:inline-block; padding:6px 12px; border-radius:999px; background:#2a1d11; color:#f4c06b; font-size:12px; font-weight:700; letter-spacing:0.6px;">
                          GİRİŞ DOĞRULAMA
                        </div>
                        <h2 style="margin:16px 0 10px; font-size:28px; color:#ffffff;">CineTrack giriş kodunuz hazır</h2>
                        <p style="margin:0 0 18px; color:#b7bccd; line-height:1.6;">
                          Hesabınıza girişi tamamlamak için aşağıdaki 6 haneli kodu kullanın.
                        </p>
                      </div>
                      <div style="padding:0 24px 24px;">
                        <div style="padding:18px 20px; border-radius:16px; background:#10111a; border:1px solid #2f3242; text-align:center;">
                          <div style="font-size:13px; color:#8f96ad; margin-bottom:8px;">Tek kullanımlık giriş kodu</div>
                          <div style="font-size:34px; font-weight:800; letter-spacing:8px; color:#ffffff;">{code}</div>
                        </div>
                        <p style="margin:18px 0 8px; color:#b7bccd; line-height:1.6;">
                          Kod {expiresInMinutes} dakika boyunca geçerlidir. Bu giriş işlemi size ait değilse bu e-postayı yok sayabilirsiniz.
                        </p>
                      </div>
                    </div>
                  </body>
                </html>
                """;
    }

    internal static PendingVerificationDto CreatePendingVerificationDto(
        string temporaryToken,
        DateTime expiresAt,
        string email) =>
        new(temporaryToken, expiresAt, email);

    internal static (string Token, DateTime ExpiresAt) GenerateTemporaryToken(
        IJwtProvider jwtProvider,
        User user,
        DateTime expiresAt) =>
        jwtProvider.GenerateTemporaryToken(
            user.Email,
            AuthTokenTypes.PendingLogin,
            user.Id,
            expiresAt);

    internal static bool TryValidateTemporaryToken(
        IJwtProvider jwtProvider,
        string temporaryToken,
        out TemporaryTokenValidationResult validationResult)
    {
        validationResult = jwtProvider.ValidateTemporaryToken(
            temporaryToken,
            AuthTokenTypes.PendingLogin);

        return validationResult.IsValid && validationResult.Payload is not null;
    }

    internal static bool IsCodeMatch(string providedCode, string expectedCode)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedCode);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedCode);

        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
}
