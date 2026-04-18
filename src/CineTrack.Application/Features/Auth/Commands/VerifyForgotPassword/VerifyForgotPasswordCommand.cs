using CineTrack.Application.Abstractions;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Auth.Commands.VerifyForgotPassword;

public record VerifyForgotPasswordCommand(string TemporaryToken, string Code)
    : IRequest<Result>, ITransactionalCommand;
