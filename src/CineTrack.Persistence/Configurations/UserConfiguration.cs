using CineTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CineTrack.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.HasMany(u => u.FavoriteMovies)
            .WithOne(f => f.User)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.FollowedActors)
            .WithOne(f => f.User)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
