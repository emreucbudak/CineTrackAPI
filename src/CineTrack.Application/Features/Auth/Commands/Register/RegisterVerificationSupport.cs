using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CineTrack.Application.Abstractions;
using CineTrack.Application.Features.Auth.Common;

namespace CineTrack.Application.Features.Auth.Commands.Register;

internal static class RegisterVerificationSupport
{
    public const int VerificationCodeLength = 6;
    public const int MaxVerificationAttempts = 5;
    public static readonly TimeSpan VerificationLifetime = TimeSpan.FromMinutes(10);

    public static string GetCacheKey(string temporaryToken) => $"auth:register:{temporaryToken}";

    public static (string Token, DateTime ExpiresAt) GenerateTemporaryToken(
        IJwtProvider jwtProvider,
        string email,
        DateTime expiresAt) =>
        jwtProvider.GenerateTemporaryToken(
            email,
            AuthTokenTypes.PendingRegister,
            expiresAt: expiresAt);

    public static bool TryValidateTemporaryToken(
        IJwtProvider jwtProvider,
        string temporaryToken,
        out TemporaryTokenValidationResult validationResult)
    {
        validationResult = jwtProvider.ValidateTemporaryToken(
            temporaryToken,
            AuthTokenTypes.PendingRegister);

        return validationResult.IsValid && validationResult.Payload is not null;
    }

    public static string GenerateVerificationCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString($"D{VerificationCodeLength}", CultureInfo.InvariantCulture);

    public static bool IsCodeMatch(string providedCode, string expectedCode)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedCode);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedCode);

        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }

    public static string BuildEmailBody(string username, string verificationCode, TimeSpan lifetime)
    {
        var totalMinutes = (int)lifetime.TotalMinutes;
        var displayName = string.IsNullOrWhiteSpace(username) ? "cinephile" : username;

        return $"""
                <html>
                  <body style="margin:0; padding:24px; background:#0f0f14; font-family:Arial, sans-serif; color:#f5f5f5;">
                    <div style="max-width:560px; margin:0 auto; background:#171821; border:1px solid #2b2d3a; border-radius:18px; overflow:hidden;">
                      <div style="padding:24px 24px 8px;">
                        <div style="display:inline-block; padding:6px 12px; border-radius:999px; background:#13251d; color:#7ee0ad; font-size:12px; font-weight:700; letter-spacing:0.6px;">
                          KAYIT DOGRULAMA
                        </div>
                        <h2 style="margin:16px 0 10px; font-size:28px; color:#ffffff;">Hos geldin {displayName}</h2>
                        <p style="margin:0 0 18px; color:#b7bccd; line-height:1.6;">
                          CineTrack kaydini tamamlamak icin asagidaki dogrulama kodunu gir.
                        </p>
                      </div>
                      <div style="padding:0 24px 24px;">
                        <div style="padding:18px 20px; border-radius:16px; background:#10111a; border:1px solid #2f3242; text-align:center;">
                          <div style="font-size:13px; color:#8f96ad; margin-bottom:8px;">Kayit kodu</div>
                          <div style="font-size:34px; font-weight:800; letter-spacing:8px; color:#ffffff;">{verificationCode}</div>
                        </div>
                        <p style="margin:18px 0 8px; color:#b7bccd; line-height:1.6;">
                          Kod {totalMinutes} dakika boyunca gecerlidir. Bu kayit islemini siz baslatmadiysaniz bu e-postayi yok sayabilirsiniz.
                        </p>
                      </div>
                    </div>
                  </body>
                </html>
                """;
    }
}
