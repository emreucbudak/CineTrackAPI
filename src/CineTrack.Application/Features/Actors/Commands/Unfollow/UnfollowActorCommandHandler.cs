using CineTrack.Application.Abstractions;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Actors.Commands.Unfollow;

public class UnfollowActorCommandHandler : IRequestHandler<UnfollowActorCommand, Result>
{
    private readonly IAppDbContext _db;

    public UnfollowActorCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UnfollowActorCommand request, CancellationToken cancellationToken)
    {
        var followed = await _db.FollowedActors
            .FirstOrDefaultAsync(f => f.UserId == request.UserId && f.TmdbId == request.TmdbId, cancellationToken);

        if (followed is null)
            return Result.Failure(Error.NotFound("FollowedActor", request.TmdbId));

        _db.FollowedActors.Remove(followed);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
