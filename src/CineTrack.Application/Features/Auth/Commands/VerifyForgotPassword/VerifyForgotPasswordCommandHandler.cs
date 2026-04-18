using System.Security.Cryptography;
using System.Text;
using CineTrack.Application.Abstractions;
using CineTrack.Application.Features.Auth.Common;
using CineTrack.Application.Features.Auth.Commands.ForgotPassword;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Auth.Commands.VerifyForgotPassword;

public class VerifyForgotPasswordCommandHandler : IRequestHandler<VerifyForgotPasswordCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IJwtProvider _jwtProvider;

    public VerifyForgotPasswordCommandHandler(
        IAppDbContext db,
        ICacheService cache,
        IJwtProvider jwtProvider)
    {
        _db = db;
        _cache = cache;
        _jwtProvider = jwtProvider;
    }

    public async Task<Result> Handle(VerifyForgotPasswordCommand request, CancellationToken cancellationToken)
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

        var remainingLifetime = pendingReset.ExpiresAtUtc - DateTime.UtcNow;
        if (remainingLifetime <= TimeSpan.Zero)
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            return Result.Failure(Error.Validation("Auth.InvalidOrExpiredVerification", "Invalid or expired verification request."));
        }

        if (!IsCodeMatch(request.Code, pendingReset.VerificationCode))
        {
            var remainingAttempts = pendingReset.RemainingAttempts - 1;

            if (remainingAttempts <= 0)
            {
                await _cache.RemoveAsync(cacheKey, cancellationToken);
            }
            else
            {
                pendingReset.RemainingAttempts = remainingAttempts;
                await _cache.SetAsync(cacheKey, pendingReset, remainingLifetime, cancellationToken);
            }

            return Result.Failure(Error.Validation("Auth.InvalidVerificationCode", "Invalid verification code."));
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

        if (pendingReset.UserId is Guid userId)
        {
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user is not null)
            {
                user.PasswordHash = pendingReset.PasswordHash;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        await _cache.RemoveAsync(cacheKey, cancellationToken);
        return Result.Success();
    }

    private static string GetCacheKey(string temporaryToken) => $"auth:forgot-password:{temporaryToken}";

    private static bool IsCodeMatch(string providedCode, string expectedCode)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedCode);
        var expectedBytes = Encoding.UTF8.GetBytes(expectedCode);

        return CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
