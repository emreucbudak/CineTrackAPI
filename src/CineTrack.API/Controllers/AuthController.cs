using CineTrack.API.Models;
using CineTrack.Application.Features.Auth.Commands.Login;
using CineTrack.Application.Features.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CineTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return StatusCode(409, ApiResponse<object>.Fail(result.Error.Code, result.Error.Message, 409));

        return StatusCode(201, ApiResponse<object>.Ok(result.Value, 201));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return Unauthorized(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message, 401));

        return Ok(ApiResponse<object>.Ok(result.Value));
    }
}
