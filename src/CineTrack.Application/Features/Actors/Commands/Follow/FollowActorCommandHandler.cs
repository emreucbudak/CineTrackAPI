using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Application.Events;
using CineTrack.Domain.Entities;
using CineTrack.Domain.Shared;
using DotNetCore.CAP;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Actors.Commands.Follow;

public class FollowActorCommandHandler : IRequestHandler<FollowActorCommand, Result<FollowedActorDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICapPublisher _capPublisher;

    public FollowActorCommandHandler(IAppDbContext db, ICapPublisher capPublisher)
    {
        _db = db;
        _capPublisher = capPublisher;
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

        await _capPublisher.PublishAsync(
            EventNames.ActorFollowed,
            new ActorFollowedEvent(request.UserId, request.TmdbId, request.Name, followed.FollowedAt),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new FollowedActorDto(followed.Id, followed.TmdbId, followed.Name, followed.ProfilePath, followed.FollowedAt);
    }
}
