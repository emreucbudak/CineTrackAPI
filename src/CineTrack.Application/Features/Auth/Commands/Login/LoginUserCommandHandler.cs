using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Auth.Commands.Login;

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<AuthTokenDto>>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;

    public LoginUserCommandHandler(
        IAppDbContext db,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
    }

    public async Task<Result<AuthTokenDto>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null)
            return Result.Failure<AuthTokenDto>(Error.Validation("Auth.InvalidCredentials", "Invalid email or password."));

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result.Failure<AuthTokenDto>(Error.Validation("Auth.InvalidCredentials", "Invalid email or password."));

        var (token, expiresAt) = _jwtProvider.GenerateToken(user);

        return new AuthTokenDto(token, expiresAt);
    }
}
