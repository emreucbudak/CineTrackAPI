namespace CineTrack.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<FavoriteMovie> FavoriteMovies { get; set; } = [];
    public ICollection<FollowedActor> FollowedActors { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
