using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Auth.Commands.Register;

public record RegisterUserCommand(string Email, string Username, string Password)
    : IRequest<Result<PendingVerificationDto>>;
