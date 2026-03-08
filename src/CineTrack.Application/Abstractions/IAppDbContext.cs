using CineTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<FavoriteMovie> FavoriteMovies { get; }
    DbSet<FollowedActor> FollowedActors { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
