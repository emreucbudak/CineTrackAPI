using CineTrack.Application.Abstractions;
using CineTrack.Application.Features.Auth.Commands.ForgotPassword;
using CineTrack.Application.Features.Auth.Common;
using CineTrack.Domain.Entities;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Auth.Commands.ResetForgotPassword;

public class ResetForgotPasswordCommandHandler : IRequestHandler<ResetForgotPasswordCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IJwtProvider _jwtProvider;
    private readonly IPasswordFingerprintService _passwordFingerprintService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRedisBloomService _redisBloomService;

    public ResetForgotPasswordCommandHandler(
        IAppDbContext db,
        ICacheService cache,
        IJwtProvider jwtProvider,
        IPasswordHasher passwordHasher,
        IPasswordFingerprintService passwordFingerprintService,
        IRedisBloomService redisBloomService)
    {
        _db = db;
        _cache = cache;
        _jwtProvider = jwtProvider;
        _passwordHasher = passwordHasher;
        _passwordFingerprintService = passwordFingerprintService;
        _redisBloomService = redisBloomService;
    }

    public async Task<Result> Handle(ResetForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var validationResult = _jwtProvider.ValidateTemporaryToken(
            request.TemporaryToken,
            AuthTokenTypes.PendingPasswordReset);

        if (!validationResult.IsValid || validationResult.Payload is null)
        {
            return Result.Failure(
                Error.Validation("Auth.InvalidTemporaryToken", "Invalid or expired password reset token."));
        }

        var cacheKey = GetCacheKey(request.TemporaryToken);
        var pendingReset = await _cache.GetAsync<ForgotPasswordCacheEntry>(cacheKey, cancellationToken);

        if (pendingReset is null)
            return Result.Failure(Error.Validation("Auth.InvalidOrExpiredVerification", "Invalid or expired verification request."));

        if (!pendingReset.IsCodeVerified)
        {
            return Result.Failure(
                Error.Validation("Auth.VerificationRequired", "Verification code must be confirmed before resetting the password."));
        }

        if (!string.Equals(
                validationResult.Payload.Email,
                pendingReset.Email,
                StringComparison.OrdinalIgnoreCase))
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            return Result.Failure(
                Error.Validation("Auth.InvalidVerificationCode", "Verification session does not match this email."));
        }

        if (pendingReset.ExpiresAtUtc <= DateTime.UtcNow)
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            return Result.Failure(Error.Validation("Auth.InvalidOrExpiredVerification", "Invalid or expired verification request."));
        }

        if (pendingReset.UserId is Guid userId)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is not null)
            {
                if (_passwordHasher.Verify(request.NewPassword, user.PasswordHash))
                {
                    return Result.Failure(
                        Error.Validation(
                            "Auth.PasswordReuseNotAllowed",
                            "New password cannot match your current password or your last 3 previous passwords."));
                }

                var bloomKey = AuthBloomFilterKeys.PasswordHistory(user.Id);
                var newPasswordFingerprint = _passwordFingerprintService.CreateFingerprint(request.NewPassword);
                var shouldVerifyRecentPasswords = await _redisBloomService.ExistsAsync(
                    bloomKey,
                    newPasswordFingerprint,
                    cancellationToken);

                var recentPasswordHistories = await _db.PasswordHistories
                    .Where(x => x.UserId == user.Id)
                    .OrderByDescending(x => x.Id)
                    .ToListAsync(cancellationToken);

                if (shouldVerifyRecentPasswords || recentPasswordHistories.Count > 0)
                {
                    var matchesRecentPassword = recentPasswordHistories
                        .Take(3)
                        .Any(x => _passwordHasher.Verify(request.NewPassword, x.PreviousPasswordHash));

                    if (matchesRecentPassword)
                    {
                        return Result.Failure(
                            Error.Validation(
                                "Auth.PasswordReuseNotAllowed",
                                "New password cannot match your current password or your last 3 previous passwords."));
                    }
                }

                _db.PasswordHistories.Add(new PasswordHistory
                {
                    UserId = user.Id,
                    PreviousPasswordHash = user.PasswordHash
                });

                var passwordHistoriesToRemove = recentPasswordHistories
                    .Skip(2)
                    .ToList();

                if (passwordHistoriesToRemove.Count > 0)
                {
                    _db.PasswordHistories.RemoveRange(passwordHistoriesToRemove);
                }

                user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
                await _db.SaveChangesAsync(cancellationToken);

                try
                {
                    await _redisBloomService.AddAsync(bloomKey, newPasswordFingerprint, cancellationToken);
                }
                catch
                {
                    // Best-effort optimization; password history table remains authoritative.
                }
            }
        }

        await _cache.RemoveAsync(cacheKey, cancellationToken);
        return Result.Success();
    }

    private static string GetCacheKey(string temporaryToken) => $"auth:forgot-password:{temporaryToken}";
}
