using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Application.Events;
using CineTrack.Application.Features.Auth.Common;
using CineTrack.Application.Features.Auth.Commands.Register;
using CineTrack.Domain.Entities;
using CineTrack.Domain.Shared;
using DotNetCore.CAP;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Auth.Commands.VerifyRegister;

public class VerifyRegisterCommandHandler : IRequestHandler<VerifyRegisterCommand, Result<UserDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICacheService _cache;
    private readonly ICapPublisher _capPublisher;
    private readonly IJwtProvider _jwtProvider;
    private readonly IRedisBloomService _redisBloomService;

    public VerifyRegisterCommandHandler(
        IAppDbContext db,
        ICacheService cache,
        ICapPublisher capPublisher,
        IJwtProvider jwtProvider,
        IRedisBloomService redisBloomService)
    {
        _db = db;
        _cache = cache;
        _capPublisher = capPublisher;
        _jwtProvider = jwtProvider;
        _redisBloomService = redisBloomService;
    }

    public async Task<Result<UserDto>> Handle(VerifyRegisterCommand request, CancellationToken cancellationToken)
    {
        if (!RegisterVerificationSupport.TryValidateTemporaryToken(
                _jwtProvider,
                request.TemporaryToken,
                out var validationResult))
        {
            return Result.Failure<UserDto>(
                Error.Validation("Auth.InvalidTemporaryToken", "Kayıt doğrulama oturumunun süresi dolmuş. Lütfen tekrar kayıt isteği oluşturun."));
        }

        var cacheKey = RegisterVerificationSupport.GetCacheKey(request.TemporaryToken);
        var pendingRegistration = await _cache.GetAsync<RegisterVerificationCacheEntry>(cacheKey, cancellationToken);

        if (pendingRegistration is null)
            return Result.Failure<UserDto>(Error.Validation("Auth.InvalidOrExpiredVerification", "Doğrulama isteğinin süresi dolmuş. Lütfen tekrar deneyin."));

        var remainingLifetime = pendingRegistration.ExpiresAtUtc - DateTime.UtcNow;
        if (remainingLifetime <= TimeSpan.Zero)
        {
            await RemovePendingRegistrationAsync(cacheKey, cancellationToken);
            return Result.Failure<UserDto>(Error.Validation("Auth.InvalidOrExpiredVerification", "Doğrulama isteğinin süresi dolmuş. Lütfen tekrar deneyin."));
        }

        if (!RegisterVerificationSupport.IsCodeMatch(request.Code, pendingRegistration.VerificationCode))
        {
            var remainingAttempts = pendingRegistration.RemainingAttempts - 1;

            if (remainingAttempts <= 0)
            {
                await RemovePendingRegistrationAsync(cacheKey, cancellationToken);
            }
            else
            {
                pendingRegistration.RemainingAttempts = remainingAttempts;
                await _cache.SetAsync(cacheKey, pendingRegistration, remainingLifetime, cancellationToken);
            }

            return Result.Failure<UserDto>(Error.Validation("Auth.InvalidVerificationCode", "Doğrulama kodu hatalı. Lütfen e-postanızdaki 6 haneli kodu kontrol edin."));
        }

        if (!string.Equals(
                validationResult.Payload!.Email,
                pendingRegistration.Email,
                StringComparison.OrdinalIgnoreCase))
        {
            await RemovePendingRegistrationAsync(cacheKey, cancellationToken);
            return Result.Failure<UserDto>(
                Error.Validation("Auth.InvalidVerificationCode", "Doğrulama oturumu bu e-posta adresiyle eşleşmiyor."));
        }

        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == pendingRegistration.Email, cancellationToken);

        if (emailExists)
        {
            await RemovePendingRegistrationAsync(cacheKey, cancellationToken);
            return Result.Failure<UserDto>(Error.Conflict("User.EmailExists", "Bu e-posta ile kayıtlı bir hesap var."));
        }

        var usernameExists = await _db.Users
            .AnyAsync(u => u.Username == pendingRegistration.Username, cancellationToken);

        if (usernameExists)
        {
            await RemovePendingRegistrationAsync(cacheKey, cancellationToken);
            return Result.Failure<UserDto>(Error.Conflict("User.UsernameExists", "Bu kullanıcı adı zaten kullanılıyor."));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = pendingRegistration.Email,
            Username = pendingRegistration.Username,
            PasswordHash = pendingRegistration.PasswordHash,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        await _capPublisher.PublishAsync(
            EventNames.UserRegistered,
            new UserRegisteredEvent(user.Id, user.Email, user.Username, user.CreatedAt),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        await AddUserBloomEntriesAsync(user, pendingRegistration.PasswordFingerprint, cancellationToken);
        await RemovePendingRegistrationAsync(cacheKey, cancellationToken);

        return new UserDto(user.Id, user.Email, user.Username, user.CreatedAt);
    }

    private async Task AddUserBloomEntriesAsync(
        User user,
        string passwordFingerprint,
        CancellationToken cancellationToken)
    {
        try
        {
            await _redisBloomService.AddAsync(
                AuthBloomFilterKeys.RegisteredEmails,
                user.Email,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(passwordFingerprint))
            {
                await _redisBloomService.AddAsync(
                    AuthBloomFilterKeys.PasswordHistory(user.Id),
                    passwordFingerprint,
                    cancellationToken);
            }
        }
        catch
        {
            // Best-effort optimization; the database remains authoritative.
        }
    }

    private async Task RemovePendingRegistrationAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
        }
        catch
        {
            // Best-effort cleanup; Redis TTL still bounds stale pending registrations.
        }
    }
}
