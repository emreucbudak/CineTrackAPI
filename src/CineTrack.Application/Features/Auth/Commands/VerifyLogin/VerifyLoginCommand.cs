using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Auth.Commands.VerifyLogin;

public record VerifyLoginCommand(string TemporaryToken, string Code) : IRequest<Result<AuthTokenDto>>;
