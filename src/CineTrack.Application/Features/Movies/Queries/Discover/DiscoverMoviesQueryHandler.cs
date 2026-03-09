using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Movies.Queries.Discover;

public class DiscoverMoviesQueryHandler : IRequestHandler<DiscoverMoviesQuery, Result<List<DiscoverMovieDto>>>
{
    private readonly ITmdbService _tmdbService;

    public DiscoverMoviesQueryHandler(ITmdbService tmdbService)
    {
        _tmdbService = tmdbService;
    }

    public async Task<Result<List<DiscoverMovieDto>>> Handle(DiscoverMoviesQuery request, CancellationToken cancellationToken)
    {
        return await _tmdbService.DiscoverMoviesAsync(request.Page, cancellationToken);
    }
}
