using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Movies.Queries.Search;

public record SearchMoviesQuery(string Query, int Page = 1)
    : IRequest<Result<List<TrendingMovieDto>>>, ICacheableQuery
{
    public string CacheKey => $"tmdb:movies:search:{Query.Trim().ToLowerInvariant()}:{Page}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
}
