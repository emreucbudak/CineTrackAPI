using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Application.Features.Auth.Commands.VerifyLogin;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Auth.Commands.Login;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<PendingVerificationDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICacheService _cache;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;

    public LoginUserCommandHandler(
        IAppDbContext db,
        ICacheService cache,
        IEmailService emailService,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider)
    {
        _db = db;
        _cache = cache;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
    }

    public async Task<Result<PendingVerificationDto>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null)
            return Result.Failure<PendingVerificationDto>(Error.Validation("Auth.InvalidCredentials", "Invalid email or password."));

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<PendingVerificationDto>(Error.Validation("Auth.InvalidCredentials", "Invalid email or password."));

        var now = DateTime.UtcNow;
        var expiresAt = now.Add(LoginVerificationSupport.VerificationCodeLifetime);
        var code = LoginVerificationSupport.GenerateVerificationCode();
        var temporaryTokenResult = LoginVerificationSupport.GenerateTemporaryToken(_jwtProvider, user, expiresAt);
        var temporaryToken = temporaryTokenResult.Token;
        var verificationCacheKey = LoginVerificationSupport.BuildVerificationCacheKey(temporaryToken);
        var latestChallengeKey = LoginVerificationSupport.BuildLatestChallengeCacheKey(user.Email);
        var previousTemporaryToken = await _cache.GetAsync<string>(latestChallengeKey, cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousTemporaryToken))
        {
            await _cache.RemoveAsync(
                LoginVerificationSupport.BuildVerificationCacheKey(previousTemporaryToken),
                cancellationToken);
        }

        var cacheItem = new PendingLoginVerificationCacheItem(user.Id, user.Email, code, expiresAt);
        await _cache.SetAsync(
            verificationCacheKey,
            cacheItem,
            LoginVerificationSupport.VerificationCodeLifetime,
            cancellationToken);
        await _cache.SetAsync(
            latestChallengeKey,
            temporaryToken,
            LoginVerificationSupport.VerificationCodeLifetime,
            cancellationToken);

        await _emailService.SendAsync(
            user.Email,
            "Your CineTrack login verification code",
            LoginVerificationSupport.BuildVerificationEmailHtml(code),
            cancellationToken);

        return LoginVerificationSupport.CreatePendingVerificationDto(
            temporaryToken,
            temporaryTokenResult.ExpiresAt,
            user.Email);
    }
}
