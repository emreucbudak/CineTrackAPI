using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Movies.Commands.RemoveFavorite;

public record RemoveFavoriteMovieCommand(Guid UserId, int TmdbId) : IRequest<Result>;
