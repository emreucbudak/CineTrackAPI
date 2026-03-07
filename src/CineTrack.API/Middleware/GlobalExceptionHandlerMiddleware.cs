using System.Net;
using System.Text.Json;
using CineTrack.API.Models;

namespace CineTrack.API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, errorCode) = exception switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, "Argument.Null"),
            ArgumentException => (HttpStatusCode.BadRequest, "Argument.Invalid"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Auth.Unauthorized"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource.NotFound"),
            _ => (HttpStatusCode.InternalServerError, "Server.InternalError")
        };

        var response = ApiResponse<object>.Fail(
            errorCode,
            exception.Message,
            (int)statusCode);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
