namespace CineTrack.Application.DTOs;

public record DiscoverMovieDto(int Id, string Title, string? Overview, string? PosterPath, string? ReleaseDate, double VoteAverage);
