using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Actors.Queries.GetFollowed;

public record GetFollowedActorsQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PaginatedResult<FollowedActorDto>>>;
