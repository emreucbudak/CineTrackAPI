using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Movies.Queries.GetTrending;

public class GetTrendingMoviesQueryHandler : IRequestHandler<GetTrendingMoviesQuery, Result<List<TrendingMovieDto>>>
{
    private readonly ITmdbService _tmdbService;

    public GetTrendingMoviesQueryHandler(ITmdbService tmdbService)
    {
        _tmdbService = tmdbService;
    }

    public async Task<Result<List<TrendingMovieDto>>> Handle(GetTrendingMoviesQuery request, CancellationToken cancellationToken)
    {
        return await _tmdbService.GetTrendingMoviesAsync(request.TimeWindow, cancellationToken);
    }
}
