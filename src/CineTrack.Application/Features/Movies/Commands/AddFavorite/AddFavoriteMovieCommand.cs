using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Movies.Commands.AddFavorite;

public record AddFavoriteMovieCommand(
    Guid UserId,
    int TmdbId,
    string Title,
    string? PosterPath) : IRequest<Result<FavoriteMovieDto>>, ITransactionalCommand;
