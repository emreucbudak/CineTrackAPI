using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email)
    : IRequest<Result<PendingVerificationDto>>;
