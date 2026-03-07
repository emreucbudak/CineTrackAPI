using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Movies.Queries.GetFavorites;

public record GetFavoriteMoviesQuery(Guid UserId) : IRequest<Result<List<FavoriteMovieDto>>>;
