using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Auth.Commands.Register;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<PendingVerificationDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IEmailService _emailService;
    private readonly IJwtProvider _jwtProvider;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(
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

    public async Task<Result<PendingVerificationDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim();
        var normalizedUsername = request.Username.Trim();

        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (emailExists)
            return Result.Failure<PendingVerificationDto>(Error.Conflict("User.EmailExists", "A user with this email already exists."));

        var usernameExists = await _db.Users
            .AnyAsync(u => u.Username == normalizedUsername, cancellationToken);

        if (usernameExists)
            return Result.Failure<PendingVerificationDto>(Error.Conflict("User.UsernameExists", "A user with this username already exists."));

        var expiresAtUtc = DateTime.UtcNow.Add(RegisterVerificationSupport.VerificationLifetime);
        var (temporaryToken, _) = RegisterVerificationSupport.GenerateTemporaryToken(
            _jwtProvider,
            normalizedEmail,
            expiresAtUtc);
        var verificationCode = RegisterVerificationSupport.GenerateVerificationCode();
        var passwordHash = _passwordHasher.Hash(request.Password);
        var cacheKey = RegisterVerificationSupport.GetCacheKey(temporaryToken);

        var cacheEntry = new RegisterVerificationCacheEntry
        {
            Email = normalizedEmail,
            Username = normalizedUsername,
            PasswordHash = passwordHash,
            VerificationCode = verificationCode,
            ExpiresAtUtc = expiresAtUtc,
            RemainingAttempts = RegisterVerificationSupport.MaxVerificationAttempts
        };

        await _cache.SetAsync(
            cacheKey,
            cacheEntry,
            RegisterVerificationSupport.VerificationLifetime,
            cancellationToken: cancellationToken);

        try
        {
            await _emailService.SendAsync(
                normalizedEmail,
                "CineTrack registration verification code",
                RegisterVerificationSupport.BuildEmailBody(normalizedUsername, verificationCode, RegisterVerificationSupport.VerificationLifetime),
                cancellationToken);
        }
        catch
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            throw;
        }

        return new PendingVerificationDto(temporaryToken, expiresAtUtc, normalizedEmail);
    }
}
