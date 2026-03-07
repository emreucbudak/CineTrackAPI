namespace CineTrack.Application.DTOs;

public record FavoriteMovieDto(Guid Id, int TmdbId, string Title, string? PosterPath, DateTime AddedAt);
