namespace CineTrack.Application.DTOs;

public record UserDto(Guid Id, string Email, string Username, DateTime CreatedAt);
