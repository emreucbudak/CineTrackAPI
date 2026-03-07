using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Actors.Queries.GetFollowed;

public class GetFollowedActorsQueryHandler : IRequestHandler<GetFollowedActorsQuery, Result<List<FollowedActorDto>>>
{
    private readonly IAppDbContext _db;

    public GetFollowedActorsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<FollowedActorDto>>> Handle(GetFollowedActorsQuery request, CancellationToken cancellationToken)
    {
        var actors = await _db.FollowedActors
            .Where(f => f.UserId == request.UserId)
            .OrderByDescending(f => f.FollowedAt)
            .Select(f => new FollowedActorDto(f.Id, f.TmdbId, f.Name, f.ProfilePath, f.FollowedAt))
            .ToListAsync(cancellationToken);

        return actors;
    }
}
