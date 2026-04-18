using CineTrack.Application.Abstractions;
using CineTrack.Application.Features.Auth.Commands.ForgotPassword;
using CineTrack.Application.Features.Auth.Common;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Auth.Commands.ResetForgotPassword;

public class ResetForgotPasswordCommandHandler : IRequestHandler<ResetForgotPasswordCommand, Result>
{
    private readonly IAppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IJwtProvider _jwtProvider;
    private readonly IPasswordHasher _passwordHasher;

    public ResetForgotPasswordCommandHandler(
        IAppDbContext db,
        ICacheService cache,
        IJwtProvider jwtProvider,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _cache = cache;
        _jwtProvider = jwtProvider;
        _passwordHasher = passwordHasher;
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
                user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        await _cache.RemoveAsync(cacheKey, cancellationToken);
        return Result.Success();
    }

    private static string GetCacheKey(string temporaryToken) => $"auth:forgot-password:{temporaryToken}";
}
