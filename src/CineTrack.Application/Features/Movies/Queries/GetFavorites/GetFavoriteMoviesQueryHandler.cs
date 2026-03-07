using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Movies.Queries.GetFavorites;

public class GetFavoriteMoviesQueryHandler : IRequestHandler<GetFavoriteMoviesQuery, Result<List<FavoriteMovieDto>>>
{
    private readonly IAppDbContext _db;

    public GetFavoriteMoviesQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<List<FavoriteMovieDto>>> Handle(GetFavoriteMoviesQuery request, CancellationToken cancellationToken)
    {
        var favorites = await _db.FavoriteMovies
            .Where(f => f.UserId == request.UserId)
            .OrderByDescending(f => f.AddedAt)
            .Select(f => new FavoriteMovieDto(f.Id, f.TmdbId, f.Title, f.PosterPath, f.AddedAt))
            .ToListAsync(cancellationToken);

        return favorites;
    }
}
