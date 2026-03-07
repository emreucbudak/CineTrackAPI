using CineTrack.Application.Abstractions;
using CineTrack.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Features.Movies.Commands.RemoveFavorite;

public class RemoveFavoriteMovieCommandHandler : IRequestHandler<RemoveFavoriteMovieCommand, Result>
{
    private readonly IAppDbContext _db;

    public RemoveFavoriteMovieCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(RemoveFavoriteMovieCommand request, CancellationToken cancellationToken)
    {
        var favorite = await _db.FavoriteMovies
            .FirstOrDefaultAsync(f => f.UserId == request.UserId && f.TmdbId == request.TmdbId, cancellationToken);

        if (favorite is null)
            return Result.Failure(Error.NotFound("FavoriteMovie", request.TmdbId));

        _db.FavoriteMovies.Remove(favorite);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
