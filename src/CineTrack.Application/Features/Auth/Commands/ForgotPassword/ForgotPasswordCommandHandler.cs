using System.Globalization;
using System.Security.Cryptography;
using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Application.Features.Auth.Common;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<PendingVerificationDto>>
{
    private const int VerificationCodeLength = 6;
    private const int MaxVerificationAttempts = 5;
    private static readonly TimeSpan VerificationLifetime = TimeSpan.FromMinutes(10);

    private readonly IAppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IEmailService _emailService;
    private readonly IJwtProvider _jwtProvider;
    private readonly IPasswordHasher _passwordHasher;

    public ForgotPasswordCommandHandler(
        IAppDbContext db,
        ICacheService cache,
        IEmailService emailService,
        IJwtProvider jwtProvider,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _cache = cache;
        _emailService = emailService;
        _jwtProvider = jwtProvider;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<PendingVerificationDto>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim();
        var expiresAtUtc = DateTime.UtcNow.Add(VerificationLifetime);
        var (temporaryToken, _) = _jwtProvider.GenerateTemporaryToken(
            normalizedEmail,
            AuthTokenTypes.PendingPasswordReset,
            expiresAt: expiresAtUtc);
        var verificationCode = GenerateVerificationCode();
        var passwordHash = _passwordHasher.Hash(request.NewPassword);

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        var cacheEntry = new ForgotPasswordCacheEntry
        {
            UserId = user?.Id,
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            VerificationCode = verificationCode,
            ExpiresAtUtc = expiresAtUtc,
            RemainingAttempts = MaxVerificationAttempts
        };

        await _cache.SetAsync(
            GetCacheKey(temporaryToken),
            cacheEntry,
            VerificationLifetime,
            cancellationToken);

        if (user is not null)
        {
            try
            {
                await _emailService.SendAsync(
                    normalizedEmail,
                    "CineTrack password reset code",
                    BuildEmailBody(verificationCode, VerificationLifetime),
                    cancellationToken);
            }
            catch
            {
                await _cache.RemoveAsync(GetCacheKey(temporaryToken), cancellationToken);
                throw;
            }
        }

        return new PendingVerificationDto(temporaryToken, expiresAtUtc, normalizedEmail);
    }

    private static string GetCacheKey(string temporaryToken) => $"auth:forgot-password:{temporaryToken}";

    private static string GenerateVerificationCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString($"D{VerificationCodeLength}", CultureInfo.InvariantCulture);

    private static string BuildEmailBody(string verificationCode, TimeSpan lifetime)
    {
        var totalMinutes = (int)lifetime.TotalMinutes;

        return $"""
                <p>Your CineTrack password reset code is:</p>
                <h2>{verificationCode}</h2>
                <p>This code expires in {totalMinutes} minutes.</p>
                <p>If you did not request this change, you can ignore this email.</p>
                """;
    }
}
