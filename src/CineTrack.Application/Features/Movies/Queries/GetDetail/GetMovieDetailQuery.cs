using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Movies.Queries.GetDetail;

public record GetMovieDetailQuery(int TmdbId)
    : IRequest<Result<MovieDetailDto>>, ICacheableQuery
{
    public string CacheKey => $"tmdb:movie:{TmdbId}";
    public TimeSpan? CacheDuration => TimeSpan.FromHours(6);
}
