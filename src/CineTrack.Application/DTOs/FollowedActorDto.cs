namespace CineTrack.Application.DTOs;

public record FollowedActorDto(Guid Id, int TmdbId, string Name, string? ProfilePath, DateTime FollowedAt);
