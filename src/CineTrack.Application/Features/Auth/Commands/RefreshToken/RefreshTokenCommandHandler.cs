using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CineTrack.Application.Features.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthTokenDto>>
{
    private readonly IAppDbContext _db;
    private readonly IJwtProvider _jwtProvider;
    private readonly IConfiguration _configuration;

    public RefreshTokenCommandHandler(IAppDbContext db, IJwtProvider jwtProvider, IConfiguration configuration)
    {
        _db = db;
        _jwtProvider = jwtProvider;
        _configuration = configuration;
    }

    public async Task<Result<AuthTokenDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Validate JWT signature (allow expired tokens)
        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, // Allow expired tokens for refresh
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!))
        };

        ClaimsPrincipal principal;
        try
        {
            principal = handler.ValidateToken(request.Token, validationParameters, out _);
        }
        catch (SecurityTokenException)
        {
            return Result.Failure<AuthTokenDto>(Error.Validation("Auth.InvalidToken", "Invalid or tampered token."));
        }

        var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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
