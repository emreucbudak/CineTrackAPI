namespace CineTrack.Application.Events;

public record ActorFollowedEvent(Guid UserId, int TmdbId, string Name, DateTime FollowedAt);
