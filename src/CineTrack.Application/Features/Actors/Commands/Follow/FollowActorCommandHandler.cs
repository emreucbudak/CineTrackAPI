using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Entities;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Actors.Commands.Follow;

public class FollowActorCommandHandler : IRequestHandler<FollowActorCommand, Result<FollowedActorDto>>
{
    private readonly IAppDbContext _db;

    public FollowActorCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<FollowedActorDto>> Handle(FollowActorCommand request, CancellationToken cancellationToken)
    {
        var alreadyFollowed = await _db.FollowedActors
            .AnyAsync(f => f.UserId == request.UserId && f.TmdbId == request.TmdbId, cancellationToken);

        if (alreadyFollowed)
            return Result.Failure<FollowedActorDto>(Error.Conflict("Actor.AlreadyFollowed", "This actor is already followed."));

        var followed = new FollowedActor
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            TmdbId = request.TmdbId,
            Name = request.Name,
            ProfilePath = request.ProfilePath,
            FollowedAt = DateTime.UtcNow
        };

        _db.FollowedActors.Add(followed);

        await _db.SaveChangesAsync(cancellationToken);

        return new FollowedActorDto(followed.Id, followed.TmdbId, followed.Name, followed.ProfilePath, followed.FollowedAt);
    }
}
