using Asp.Versioning;
using CineTrack.API.Models;
using CineTrack.Application.Features.Actors.Commands.Follow;
using CineTrack.Application.Features.Actors.Commands.Unfollow;
using CineTrack.Application.Features.Actors.Queries.GetDetail;
using CineTrack.Application.Features.Actors.Queries.GetFollowed;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineTrack.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ActorsController : BaseApiController
{
    private readonly ISender _sender;

    public ActorsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{personId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDetail(int personId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPersonDetailQuery(personId), cancellationToken);

        if (result.IsFailure)
            return NotFound(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message, 404));

        return Ok(ApiResponse<object>.Ok(result.Value));
    }

    [HttpGet("followed")]
    public async Task<IActionResult> GetFollowed([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetFollowedActorsQuery(GetUserId(), page, pageSize), cancellationToken);

        if (result.IsFailure)
            return BadRequest(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message));

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
