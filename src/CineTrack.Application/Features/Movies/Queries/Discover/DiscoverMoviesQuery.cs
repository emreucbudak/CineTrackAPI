using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Movies.Queries.Discover;

public record DiscoverMoviesQuery(int Page = 1)
    : IRequest<Result<List<DiscoverMovieDto>>>, ICacheableQuery
{
    public string CacheKey => $"tmdb:discover:page:{Page}";
    public TimeSpan? CacheDuration => TimeSpan.FromHours(3);
}
