using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Movies.Queries.GetDetail;

public class GetMovieDetailQueryHandler : IRequestHandler<GetMovieDetailQuery, Result<MovieDetailDto>>
{
    private readonly ITmdbService _tmdbService;

    public GetMovieDetailQueryHandler(ITmdbService tmdbService)
    {
        _tmdbService = tmdbService;
    }

    public async Task<Result<MovieDetailDto>> Handle(GetMovieDetailQuery request, CancellationToken cancellationToken)
    {
        return await _tmdbService.GetMovieDetailAsync(request.TmdbId, cancellationToken);
    }
}
