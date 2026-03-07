using System.Security.Claims;
using CineTrack.API.Models;
using CineTrack.Application.Features.Movies.Commands.AddFavorite;
using CineTrack.Application.Features.Movies.Commands.RemoveFavorite;
using CineTrack.Application.Features.Movies.Queries.GetFavorites;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineTrack.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MoviesController : ControllerBase
{
    private readonly ISender _sender;

    public MoviesController(ISender sender)
    {
        _sender = sender;
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetFavoriteMoviesQuery(GetUserId()), cancellationToken);
        return Ok(ApiResponse<object>.Ok(result.Value));
    }

    [HttpPost("favorites")]
    public async Task<IActionResult> AddFavorite(AddFavoriteMovieRequest request, CancellationToken cancellationToken)
    {
        var command = new AddFavoriteMovieCommand(GetUserId(), request.TmdbId, request.Title, request.PosterPath);
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
            return Conflict(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message, 409));

        return StatusCode(201, ApiResponse<object>.Ok(result.Value, 201));
    }

    [HttpDelete("favorites/{tmdbId:int}")]
    public async Task<IActionResult> RemoveFavorite(int tmdbId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RemoveFavoriteMovieCommand(GetUserId(), tmdbId), cancellationToken);

        if (result.IsFailure)
            return NotFound(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message, 404));

        return NoContent();
    }
}

public record AddFavoriteMovieRequest(int TmdbId, string Title, string? PosterPath);
