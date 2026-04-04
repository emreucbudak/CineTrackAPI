using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokenDto>>
{
    private readonly IAppDbContext _db;
    private readonly IJwtProvider _jwtProvider;

    public RefreshTokenCommandHandler(IAppDbContext db, IJwtProvider jwtProvider)
    {
        _db = db;
        _jwtProvider = jwtProvider;
    }

    public async Task<Result<AuthTokenDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Extract user ID from expired JWT (without validating lifetime)
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(request.Token))
            return Result.Failure<AuthTokenDto>(Error.Validation("Auth.InvalidToken", "Invalid token."));

        var jwtToken = handler.ReadJwtToken(request.Token);
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
            return Result.Failure<AuthTokenDto>(Error.Validation("Auth.InvalidToken", "Invalid token."));

        // Find and validate refresh token
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == userId, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
            return Result.Failure<AuthTokenDto>(Error.Validation("Auth.InvalidRefreshToken", "Invalid or expired refresh token."));

        // Revoke old refresh token
        storedToken.RevokedAt = DateTime.UtcNow;

        // Get user and generate new tokens
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return Result.Failure<AuthTokenDto>(Error.Validation("Auth.UserNotFound", "User not found."));

        var (newToken, expiresAt) = _jwtProvider.GenerateToken(user);

        var newRefreshToken = new Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = _jwtProvider.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthTokenDto(newToken, expiresAt, newRefreshToken.Token, newRefreshToken.ExpiresAt);
    }
}
