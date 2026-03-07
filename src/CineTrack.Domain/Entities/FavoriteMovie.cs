namespace CineTrack.Domain.Entities;

public class FavoriteMovie : BaseEntity
{
    public Guid UserId { get; set; }
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? PosterPath { get; set; }
    public DateTime AddedAt { get; set; }

    public User User { get; set; } = null!;
}
