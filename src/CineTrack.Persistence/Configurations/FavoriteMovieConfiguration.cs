using CineTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineTrack.Persistence.Configurations;

public class FavoriteMovieConfiguration : IEntityTypeConfiguration<FavoriteMovie>
{
    public void Configure(EntityTypeBuilder<FavoriteMovie> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.TmdbId)
            .IsRequired();

        builder.Property(f => f.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(f => f.PosterPath)
            .HasMaxLength(500);

        builder.Property(f => f.AddedAt)
            .IsRequired();

        builder.HasIndex(f => new { f.UserId, f.TmdbId })
            .IsUnique();

        builder.HasIndex(f => new { f.UserId, f.AddedAt });
    }
}
