using System.Text.Json.Serialization;

namespace CineTrack.API.Models;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public int StatusCode { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, int statusCode = 200) => new()
    {
        Success = true,
        Data = data,
        StatusCode = statusCode
    };

    public static ApiResponse<T> Fail(string errorCode, string errorMessage, int statusCode = 400) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        StatusCode = statusCode
    };
}
