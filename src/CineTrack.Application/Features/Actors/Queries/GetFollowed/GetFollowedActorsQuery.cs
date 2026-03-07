using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Actors.Queries.GetFollowed;

public record GetFollowedActorsQuery(Guid UserId) : IRequest<Result<List<FollowedActorDto>>>;
