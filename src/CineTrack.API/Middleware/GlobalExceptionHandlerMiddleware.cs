using System.Net;
using System.Text.Json;
using CineTrack.API.Models;
using FluentValidation;

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
            var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            _logger.LogError(ex, "Unhandled exception occurred. TraceId: {TraceId}, UserId: {UserId}, Path: {Path}",
                context.TraceIdentifier, userId, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .Select(e => $"{e.PropertyName}: {e.ErrorMessage}");

            var validationResponse = ApiResponse<object>.Fail(
                "Validation.Failed",
                string.Join("; ", errors),
                (int)HttpStatusCode.BadRequest);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync(JsonSerializer.Serialize(validationResponse, JsonOptions));
            return;
        }

        var (statusCode, errorCode) = exception switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, "Argument.Null"),
            ArgumentException => (HttpStatusCode.BadRequest, "Argument.Invalid"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Auth.Unauthorized"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource.NotFound"),
            _ => (HttpStatusCode.InternalServerError, "Server.InternalError")
        };

        var message = statusCode == HttpStatusCode.InternalServerError
            ? "An unexpected error occurred. Please try again later."
            : exception.Message;

        var response = ApiResponse<object>.Fail(
            errorCode,
            message,
            (int)statusCode);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
