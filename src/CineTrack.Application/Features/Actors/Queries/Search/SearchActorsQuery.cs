using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Actors.Queries.Search;

public record SearchActorsQuery(string Query, int Page = 1)
    : IRequest<Result<List<SearchPersonDto>>>, ICacheableQuery
{
    public string CacheKey => $"tmdb:actors:search:{Query.Trim().ToLowerInvariant()}:{Page}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(15);
}
