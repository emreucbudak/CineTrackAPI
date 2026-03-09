using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Actors.Queries.GetDetail;

public record GetPersonDetailQuery(int PersonId)
    : IRequest<Result<PersonDetailDto>>, ICacheableQuery
{
    public string CacheKey => $"tmdb:person:{PersonId}";
    public TimeSpan? CacheDuration => TimeSpan.FromHours(12);
}
