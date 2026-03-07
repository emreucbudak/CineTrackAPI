using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Actors.Commands.Follow;

public record FollowActorCommand(
    Guid UserId,
    int TmdbId,
    string Name,
    string? ProfilePath) : IRequest<Result<FollowedActorDto>>, ITransactionalCommand;
