using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Auth.Commands.Login;

public record LoginUserCommand(string Email, string Password) : IRequest<Result<PendingVerificationDto>>;
