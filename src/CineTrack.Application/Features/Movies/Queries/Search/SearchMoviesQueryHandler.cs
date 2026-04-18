using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Movies.Queries.Search;

public class SearchMoviesQueryHandler : IRequestHandler<SearchMoviesQuery, Result<List<TrendingMovieDto>>>
{
    private readonly ITmdbService _tmdbService;

    public SearchMoviesQueryHandler(ITmdbService tmdbService)
    {
        _tmdbService = tmdbService;
    }

    public async Task<Result<List<TrendingMovieDto>>> Handle(SearchMoviesQuery request, CancellationToken cancellationToken)
    {
        return await _tmdbService.SearchMoviesAsync(request.Query, request.Page, cancellationToken);
    }
}
