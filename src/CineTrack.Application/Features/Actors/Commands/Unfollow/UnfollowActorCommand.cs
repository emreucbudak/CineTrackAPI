using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Actors.Commands.Unfollow;

public record UnfollowActorCommand(Guid UserId, int TmdbId) : IRequest<Result>;
