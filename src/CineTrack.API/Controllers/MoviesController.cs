using Asp.Versioning;
using CineTrack.API.Models;
using CineTrack.Application.Features.Movies.Commands.AddFavorite;
using CineTrack.Application.Features.Movies.Commands.RemoveFavorite;
using CineTrack.Application.Features.Movies.Queries.GetDetail;
using CineTrack.Application.Features.Movies.Queries.GetFavorites;
using CineTrack.Application.Features.Movies.Queries.Discover;
using CineTrack.Application.Features.Movies.Queries.Search;
using CineTrack.Application.Features.Movies.Queries.GetTrending;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineTrack.API.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class MoviesController : BaseApiController
{
    private readonly ISender _sender;

    public MoviesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("trending")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTrending([FromQuery] string timeWindow = "day", CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetTrendingMoviesQuery(timeWindow), cancellationToken);

        if (result.IsFailure)
            return BadRequest(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message));

        return Ok(ApiResponse<object>.Ok(result.Value));
    }

    [HttpGet("{tmdbId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDetail(int tmdbId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMovieDetailQuery(tmdbId), cancellationToken);

        if (result.IsFailure)
            return NotFound(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message, 404));

        return Ok(ApiResponse<object>.Ok(result.Value));
    }

    [HttpGet("discover")]
    [AllowAnonymous]
    public async Task<IActionResult> Discover([FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new DiscoverMoviesQuery(page), cancellationToken);

        if (result.IsFailure)
            return BadRequest(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message));

        return Ok(ApiResponse<object>.Ok(result.Value));
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchMoviesQuery(query, page), cancellationToken);

        if (result.IsFailure)
            return BadRequest(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message));

        return Ok(ApiResponse<object>.Ok(result.Value));
    }

    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetFavoriteMoviesQuery(GetUserId(), page, pageSize), cancellationToken);

        if (result.IsFailure)
            return BadRequest(ApiResponse<object>.Fail(result.Error.Code, result.Error.Message));

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
