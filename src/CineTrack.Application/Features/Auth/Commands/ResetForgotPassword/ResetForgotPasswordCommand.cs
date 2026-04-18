using CineTrack.Application.Abstractions;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Auth.Commands.ResetForgotPassword;

public record ResetForgotPasswordCommand(string TemporaryToken, string NewPassword)
    : IRequest<Result>, ITransactionalCommand;
