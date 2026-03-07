namespace CineTrack.Domain.Entities;

public class FollowedActor : BaseEntity
{
    public Guid UserId { get; set; }
    public int TmdbId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProfilePath { get; set; }
    public DateTime FollowedAt { get; set; }

    public User User { get; set; } = null!;
}
