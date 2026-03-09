namespace CineTrack.Application.DTOs;

public record MovieCreditDto(int Id, string Title, string? Character, string? PosterPath, string? ReleaseDate, double VoteAverage);
