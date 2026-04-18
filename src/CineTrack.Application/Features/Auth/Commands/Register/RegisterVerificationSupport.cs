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
        var displayName = string.IsNullOrWhiteSpace(username) ? "there" : username;

        return $"""
                <p>Hi {displayName},</p>
                <p>Your CineTrack registration verification code is:</p>
                <h2>{verificationCode}</h2>
                <p>This code expires in {totalMinutes} minutes.</p>
                <p>If you did not start this registration, you can ignore this email.</p>
                """;
    }
}
