namespace CineTrack.Application.DTOs;

public record MovieDetailDto(
    int Id,
    string Title,
    string? Overview,
    string? PosterPath,
    string? BackdropPath,
    string? ReleaseDate,
    double VoteAverage,
    int VoteCount,
    List<GenreDto> Genres);

public record GenreDto(int Id, string Name);
