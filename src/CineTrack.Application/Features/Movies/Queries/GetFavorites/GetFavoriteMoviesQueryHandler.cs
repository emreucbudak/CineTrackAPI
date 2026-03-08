using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Movies.Queries.GetFavorites;

public class GetFavoriteMoviesQueryHandler : IRequestHandler<GetFavoriteMoviesQuery, Result<PaginatedResult<FavoriteMovieDto>>>
{
    private readonly IAppDbContext _db;

    public GetFavoriteMoviesQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PaginatedResult<FavoriteMovieDto>>> Handle(GetFavoriteMoviesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.FavoriteMovies
            .Where(f => f.UserId == request.UserId)
            .OrderByDescending(f => f.AddedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var favorites = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(f => new FavoriteMovieDto(f.Id, f.TmdbId, f.Title, f.PosterPath, f.AddedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<FavoriteMovieDto>(favorites, request.Page, request.PageSize, totalCount, totalPages);
    }
}
