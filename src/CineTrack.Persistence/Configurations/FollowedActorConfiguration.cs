using CineTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineTrack.Persistence.Configurations;

public class FollowedActorConfiguration : IEntityTypeConfiguration<FollowedActor>
{
    public void Configure(EntityTypeBuilder<FollowedActor> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.TmdbId)
            .IsRequired();

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.ProfilePath)
            .HasMaxLength(500);

        builder.Property(f => f.FollowedAt)
            .IsRequired();

        builder.HasIndex(f => new { f.UserId, f.TmdbId })
            .IsUnique();
    }
}
