using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Application.Events;
using CineTrack.Domain.Entities;
using CineTrack.Domain.Shared;
using DotNetCore.CAP;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Auth.Commands.Register;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICapPublisher _capPublisher;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterUserCommandHandler(
        IAppDbContext db,
        ICapPublisher capPublisher,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _capPublisher = capPublisher;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<UserDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (emailExists)
            return Result.Failure<UserDto>(Error.Conflict("User.EmailExists", "A user with this email already exists."));

        var usernameExists = await _db.Users
            .AnyAsync(u => u.Username == request.Username, cancellationToken);

        if (usernameExists)
            return Result.Failure<UserDto>(Error.Conflict("User.UsernameExists", "A user with this username already exists."));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Username = request.Username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        // CAP outbox: DB save + event publish in the same transaction
        await _capPublisher.PublishAsync(
            EventNames.UserRegistered,
            new UserRegisteredEvent(user.Id, user.Email, user.Username, user.CreatedAt),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new UserDto(user.Id, user.Email, user.Username, user.CreatedAt);
    }
}
