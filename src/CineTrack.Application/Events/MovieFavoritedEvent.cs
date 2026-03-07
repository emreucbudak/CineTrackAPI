namespace CineTrack.Application.Events;

public record MovieFavoritedEvent(Guid UserId, int TmdbId, string Title, DateTime FavoritedAt);
