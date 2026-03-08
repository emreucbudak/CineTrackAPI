using CineTrack.Application.DTOs;
using CineTrack.Domain.Shared;
using MediatR;

namespace CineTrack.Application.Features.Movies.Queries.GetFavorites;

public record GetFavoriteMoviesQuery(Guid UserId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PaginatedResult<FavoriteMovieDto>>>;
