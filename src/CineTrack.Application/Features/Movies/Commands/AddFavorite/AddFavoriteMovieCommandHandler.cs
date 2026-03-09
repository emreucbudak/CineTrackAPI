using CineTrack.Application.Abstractions;
using CineTrack.Application.DTOs;
using CineTrack.Application.Events;
using CineTrack.Domain.Entities;
using CineTrack.Domain.Shared;
using DotNetCore.CAP;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Movies.Commands.AddFavorite;

public class AddFavoriteMovieCommandHandler : IRequestHandler<AddFavoriteMovieCommand, Result<FavoriteMovieDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICapPublisher _capPublisher;

    public AddFavoriteMovieCommandHandler(IAppDbContext db, ICapPublisher capPublisher)
    {
        _db = db;
        _capPublisher = capPublisher;
    }

    public async Task<Result<FavoriteMovieDto>> Handle(AddFavoriteMovieCommand request, CancellationToken cancellationToken)
    {
        var alreadyExists = await _db.FavoriteMovies
            .AnyAsync(f => f.UserId == request.UserId && f.TmdbId == request.TmdbId, cancellationToken);

        if (alreadyExists)
            return Result.Failure<FavoriteMovieDto>(Error.Conflict("Movie.AlreadyFavorited", "This movie is already in favorites."));

        var favorite = new FavoriteMovie
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            TmdbId = request.TmdbId,
            Title = request.Title,
            PosterPath = request.PosterPath,
            AddedAt = DateTime.UtcNow
        };

        _db.FavoriteMovies.Add(favorite);

        await _capPublisher.PublishAsync(
            EventNames.MovieFavorited,
            new MovieFavoritedEvent(request.UserId, request.TmdbId, request.Title, favorite.AddedAt),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return new FavoriteMovieDto(favorite.Id, favorite.TmdbId, favorite.Title, favorite.PosterPath, favorite.AddedAt);
    }
}
