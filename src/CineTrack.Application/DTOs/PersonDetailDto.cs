namespace CineTrack.Application.DTOs;

public record PersonDetailDto(
    int Id,
    string Name,
    string? Biography,
    string? ProfilePath,
    string? Birthday,
    string? PlaceOfBirth,
    string? KnownForDepartment);
