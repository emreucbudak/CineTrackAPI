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
    internal static readonly TimeSpan VerificationCodeLifetime = TimeSpan.FromMinutes(10);

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
                  <body style="font-family: Arial, sans-serif; color: #111827;">
                    <h2 style="margin-bottom: 16px;">Verify your CineTrack login</h2>
                    <p style="margin-bottom: 12px;">Use the following 6-digit code to finish signing in:</p>
                    <p style="font-size: 32px; font-weight: 700; letter-spacing: 6px; margin: 20px 0;">{code}</p>
                    <p style="margin-bottom: 12px;">This code expires in {expiresInMinutes} minutes.</p>
                    <p style="color: #6b7280; margin: 0;">If you did not try to sign in, you can ignore this email.</p>
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
