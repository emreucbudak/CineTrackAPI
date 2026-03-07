using System.Security.Claims;
using CineTrack.API.Models;
using CineTrack.Application.Features.Actors.Commands.Follow;
using CineTrack.Application.Features.Actors.Commands.Unfollow;
using CineTrack.Application.Features.Actors.Queries.GetFollowed;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActorsController : ControllerBase
{
    private readonly ISender _sender;

    public ActorsController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("followed")]
    public async Task<IActionResult> GetFollowed(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetFollowedActorsQuery(GetUserId()), cancellationToken);
        return Ok(ApiResponse<object>.Ok(result.Value));
    }

    [HttpPost("followed")]
    public async Task<IActionResult> Follow(FollowActorRequest request, CancellationToken cancellationToken)
    {
        var command = new FollowActorCommand(GetUserId(), request.TmdbId, request.Name, request.ProfilePath);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return Conflict(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message, 409));

        return StatusCode(201, ApiResponse<object>.Ok(result.Value, 201));
    }

    [HttpDelete("followed/{tmdbId:int}")]
    public async Task<IActionResult> Unfollow(int tmdbId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UnfollowActorCommand(GetUserId(), tmdbId), cancellationToken);

        if (result.IsFailure)
            return NotFound(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message, 404));

        return NoContent();
    }
}

public record FollowActorRequest(int TmdbId, string Name, string? ProfilePath);
