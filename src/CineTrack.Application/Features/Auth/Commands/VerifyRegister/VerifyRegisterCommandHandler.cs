using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Application.Events;
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

    public VerifyRegisterCommandHandler(
        IAppDbContext db,
        ICacheService cache,
        ICapPublisher capPublisher,
        IJwtProvider jwtProvider)
    {
        _db = db;
        _cache = cache;
        _capPublisher = capPublisher;
        _jwtProvider = jwtProvider;
    }

    public async Task<Result<UserDto>> Handle(VerifyRegisterCommand request, CancellationToken cancellationToken)
    {
        if (!RegisterVerificationSupport.TryValidateTemporaryToken(
                _jwtProvider,
                request.TemporaryToken,
                out var validationResult))
        {
            return Result.Failure<UserDto>(
                Error.Validation("Auth.InvalidTemporaryToken", "Invalid or expired temporary registration token."));
        }

        var cacheKey = RegisterVerificationSupport.GetCacheKey(request.TemporaryToken);
        var pendingRegistration = await _cache.GetAsync<RegisterVerificationCacheEntry>(cacheKey, cancellationToken);

        if (pendingRegistration is null)
            return Result.Failure<UserDto>(Error.Validation("Auth.InvalidOrExpiredVerification", "Invalid or expired verification request."));

        var remainingLifetime = pendingRegistration.ExpiresAtUtc - DateTime.UtcNow;
        if (remainingLifetime <= TimeSpan.Zero)
        {
            await RemovePendingRegistrationAsync(cacheKey, cancellationToken);
            return Result.Failure<UserDto>(Error.Validation("Auth.InvalidOrExpiredVerification", "Invalid or expired verification request."));
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

            return Result.Failure<UserDto>(Error.Validation("Auth.InvalidVerificationCode", "Invalid verification code."));
        }

        if (!string.Equals(
                validationResult.Payload!.Email,
                pendingRegistration.Email,
                StringComparison.OrdinalIgnoreCase))
        {
            await RemovePendingRegistrationAsync(cacheKey, cancellationToken);
            return Result.Failure<UserDto>(
                Error.Validation("Auth.InvalidVerificationCode", "Verification session does not match this email."));
        }

        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == pendingRegistration.Email, cancellationToken);

        if (emailExists)
        {
            await RemovePendingRegistrationAsync(cacheKey, cancellationToken);
            return Result.Failure<UserDto>(Error.Conflict("User.EmailExists", "A user with this email already exists."));
        }

        var usernameExists = await _db.Users
            .AnyAsync(u => u.Username == pendingRegistration.Username, cancellationToken);

        if (usernameExists)
        {
            await RemovePendingRegistrationAsync(cacheKey, cancellationToken);
            return Result.Failure<UserDto>(Error.Conflict("User.UsernameExists", "A user with this username already exists."));
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
        await RemovePendingRegistrationAsync(cacheKey, cancellationToken);

        return new UserDto(user.Id, user.Email, user.Username, user.CreatedAt);
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
