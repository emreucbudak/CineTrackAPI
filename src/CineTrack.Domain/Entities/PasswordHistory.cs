namespace CineTrack.Domain.Entities;

public class PasswordHistory
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public string PreviousPasswordHash { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}
