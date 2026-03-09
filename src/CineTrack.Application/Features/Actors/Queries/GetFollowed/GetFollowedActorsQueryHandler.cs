using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Actors.Queries.GetFollowed;

public class GetFollowedActorsQueryHandler : IRequestHandler<GetFollowedActorsQuery, Result<PaginatedResult<FollowedActorDto>>>
{
    private readonly IAppDbContext _db;

    public GetFollowedActorsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PaginatedResult<FollowedActorDto>>> Handle(GetFollowedActorsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.FollowedActors
            .Where(f => f.UserId == request.UserId)
            .OrderByDescending(f => f.FollowedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var actors = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(f => new FollowedActorDto(f.Id, f.TmdbId, f.Name, f.ProfilePath, f.FollowedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<FollowedActorDto>(actors, request.Page, request.PageSize, totalCount, totalPages);
    }
}
