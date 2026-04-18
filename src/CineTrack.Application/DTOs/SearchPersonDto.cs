namespace CineTrack.Application.DTOs;

public record SearchPersonDto(
    int Id,
    string Name,
    string? ProfilePath,
    string? KnownForDepartment);
