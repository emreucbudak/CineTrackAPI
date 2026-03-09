namespace CineTrack.Application.DTOs;

public record TrendingMovieDto(
    int Id,
    string Title,
    string? Overview,
    string? PosterPath,
    string? ReleaseDate,
    double VoteAverage);
