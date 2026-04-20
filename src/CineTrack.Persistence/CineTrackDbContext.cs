using CineTrack.Application.Abstractions;
using CineTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CineTrack.Persistence;

public class CineTrackDbContext : DbContext, IAppDbContext
{
    public CineTrackDbContext(DbContextOptions<CineTrackDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<FavoriteMovie> FavoriteMovies => Set<FavoriteMovie>();
    public DbSet<FollowedActor> FollowedActors => Set<FollowedActor>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CineTrackDbContext).Assembly);
    }
}
