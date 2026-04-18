using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Entities;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Auth.Commands.VerifyLogin;

public class VerifyLoginCommandHandler : IRequestHandler<VerifyLoginCommand, Result<AuthTokenDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IJwtProvider _jwtProvider;

    public VerifyLoginCommandHandler(
        IAppDbContext db,
        ICacheService cache,
        IJwtProvider jwtProvider)
    {
        _db = db;
        _cache = cache;
        _jwtProvider = jwtProvider;
    }

    public async Task<Result<AuthTokenDto>> Handle(VerifyLoginCommand request, CancellationToken cancellationToken)
    {
        if (!LoginVerificationSupport.TryValidateTemporaryToken(
                _jwtProvider,
                request.TemporaryToken,
                out var validationResult))
        {
            return Result.Failure<AuthTokenDto>(
                Error.Validation("Auth.InvalidTemporaryToken", "Invalid or expired temporary login token."));
        }

        var verificationCacheKey = LoginVerificationSupport.BuildVerificationCacheKey(request.TemporaryToken);
        var cacheItem = await _cache.GetAsync<PendingLoginVerificationCacheItem>(verificationCacheKey, cancellationToken);

        if (cacheItem is null)
        {
            return Result.Failure<AuthTokenDto>(
                Error.Validation("Auth.InvalidVerificationSession", "Login verification session not found or expired."));
        }

        if (cacheItem.ExpiresAt <= DateTime.UtcNow)
        {
            await ClearVerificationCacheAsync(request.TemporaryToken, cacheItem.Email, cancellationToken);

            return Result.Failure<AuthTokenDto>(
                Error.Validation("Auth.VerificationExpired", "The login verification code has expired."));
        }

        if (!LoginVerificationSupport.IsCodeMatch(request.Code.Trim(), cacheItem.Code))
        {
            return Result.Failure<AuthTokenDto>(
                Error.Validation("Auth.InvalidVerificationCode", "Invalid verification code."));
        }

        if (!string.Equals(
                validationResult.Payload!.Email,
                cacheItem.Email,
                StringComparison.OrdinalIgnoreCase) ||
            validationResult.Payload.UserId != cacheItem.UserId)
        {
            await ClearVerificationCacheAsync(request.TemporaryToken, cacheItem.Email, cancellationToken);

            return Result.Failure<AuthTokenDto>(
                Error.Validation("Auth.InvalidVerificationSession", "Login verification session does not match the requested user."));
        }

        await ClearVerificationCacheAsync(request.TemporaryToken, cacheItem.Email, cancellationToken);

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == cacheItem.UserId && u.Email == cacheItem.Email, cancellationToken);

        if (user is null)
        {
            return Result.Failure<AuthTokenDto>(
                Error.Validation("Auth.UserNotFound", "User not found."));
        }

        var (token, expiresAt) = _jwtProvider.GenerateToken(user);
        var refreshToken = CreateRefreshToken(user);

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthTokenDto(token, expiresAt, refreshToken.Token, refreshToken.ExpiresAt);
    }

    private async Task ClearVerificationCacheAsync(
        string temporaryToken,
        string email,
        CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(
            LoginVerificationSupport.BuildVerificationCacheKey(temporaryToken),
            cancellationToken);
        await _cache.RemoveAsync(
            LoginVerificationSupport.BuildLatestChallengeCacheKey(email),
            cancellationToken);
    }

    private CineTrack.Domain.Entities.RefreshToken CreateRefreshToken(User user) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = _jwtProvider.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };
}
