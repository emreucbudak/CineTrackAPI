using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Auth.Commands.VerifyRegister;

public record VerifyRegisterCommand(string TemporaryToken, string Code)
    : IRequest<Result<UserDto>>, ITransactionalCommand;
