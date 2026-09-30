using System.Net;
using System.Text.Json;
using DevTrack.Application.Common.Exceptions;

namespace DevTrack.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            var (statusCode, message) = MapException(ex);

            if (statusCode == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning("{ExceptionType} on {Method} {Path}: {Message}", ex.GetType().Name, context.Request.Method, context.Request.Path, ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var body = JsonSerializer.Serialize(new { status = (int)statusCode, message });
            await context.Response.WriteAsync(body);
        }
    }

    private static (HttpStatusCode, string) MapException(Exception ex) => ex switch
    {
        KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
        InvalidCredentialsException => (HttpStatusCode.Unauthorized, ex.Message),
        UnauthorizedAccessException => (HttpStatusCode.Forbidden, ex.Message),
        ConflictException => (HttpStatusCode.Conflict, ex.Message),
        InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message),
        _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
    };
}