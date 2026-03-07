using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Movies.Queries.GetTrending;

public record GetTrendingMoviesQuery(string TimeWindow = "day")
    : IRequest<Result<List<TrendingMovieDto>>>, ICacheableQuery
{
    public string CacheKey => $"tmdb:trending:{TimeWindow}";
    public TimeSpan? CacheDuration => TimeSpan.FromHours(1);
}
