using Asp.Versioning;
using CineTrack.API.Models;
using CineTrack.Application.Features.Auth.Commands.ForgotPassword;
using CineTrack.Application.Features.Auth.Commands.Login;
using CineTrack.Application.Features.Auth.Commands.RefreshToken;
using CineTrack.Application.Features.Auth.Commands.Register;
using CineTrack.Application.Features.Auth.Commands.ResetForgotPassword;
using CineTrack.Application.Features.Auth.Commands.RevokeToken;
using CineTrack.Application.Features.Auth.Commands.VerifyForgotPassword;
using CineTrack.Application.Features.Auth.Commands.VerifyLogin;
using CineTrack.Application.Features.Auth.Commands.VerifyRegister;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CineTrack.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return Unauthorized(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message, 401));

        return Accepted(ApiResponse<object>.Ok(result.Value, 202));
    }

    [HttpPost("login/verify")]
    public async Task<IActionResult> VerifyLogin([FromBody] VerifyLoginCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return Unauthorized(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message, 401));

        return Ok(ApiResponse<object>.Ok(result.Value));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return StatusCode(409, ApiResponse<object>.Fail(result.Error.Code, result.Error.Message, 409));

        return Accepted(ApiResponse<object>.Ok(result.Value, 202));
    }

    [HttpPost("register/verify")]
    public async Task<IActionResult> VerifyRegister([FromBody] VerifyRegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message));

        return StatusCode(201, ApiResponse<object>.Ok(result.Value, 201));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message));

        return Accepted(ApiResponse<object>.Ok(result.Value, 202));
    }

    [HttpPost("forgot-password/verify")]
    public async Task<IActionResult> VerifyForgotPassword([FromBody] VerifyForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message));

        return Ok(ApiResponse<object>.Ok(new
        {
            message = "Şifre yenileme kodu doğrulandı."
        }));
    }

    [HttpPost("forgot-password/reset")]
    public async Task<IActionResult> ResetForgotPassword([FromBody] ResetForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message));

        return Ok(ApiResponse<object>.Ok(new
        {
            message = "Şifreniz başarıyla güncellendi."
        }));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return Unauthorized(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message, 401));

        return Ok(ApiResponse<object>.Ok(result.Value));
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return BadRequest(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message));

        return NoContent();
    }
}
