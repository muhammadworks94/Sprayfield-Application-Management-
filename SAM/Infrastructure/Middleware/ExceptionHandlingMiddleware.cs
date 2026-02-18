using System.Net;
using System.Security.Claims;
using System.Text.Json;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.Services.Models;

namespace SAM.Infrastructure.Middleware;

/// <summary>
/// Global exception handling middleware.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, IErrorLogService errorLogService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, errorLogService);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, IErrorLogService errorLogService)
    {
        _logger.LogError(exception, "An unhandled exception occurred. RequestId: {RequestId}", 
            context.TraceIdentifier);

        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            EntityNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            UnauthorizedResourceAccessException => (HttpStatusCode.Forbidden, exception.Message),
            ValidationException => (HttpStatusCode.BadRequest, exception.Message),
            BusinessRuleException => (HttpStatusCode.BadRequest, exception.Message),
            _ => (HttpStatusCode.InternalServerError, 
                _environment.IsDevelopment() ? exception.ToString() : "An error occurred while processing your request.")
        };

        await TryPersistErrorLogAsync(context, exception, (int)statusCode, errorLogService);

        response.StatusCode = (int)statusCode;

        var errorResponse = new
        {
            statusCode = (int)statusCode,
            message = message,
            requestId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(json);
    }

    private async Task TryPersistErrorLogAsync(HttpContext context, Exception exception, int statusCode, IErrorLogService errorLogService)
    {
        try
        {
            var actorUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var actorEmail = context.User.Identity?.Name;
            var actorDisplayName = context.User.FindFirstValue("FullName") ?? context.User.FindFirstValue(ClaimTypes.Name);
            var module = ResolveModuleName(context.Request.Path.Value);

            await errorLogService.CreateAsync(new ErrorLogCreateModel
            {
                OccurredAtUtc = DateTime.UtcNow,
                ExceptionType = exception.GetType().Name,
                Message = exception.Message,
                StatusCode = statusCode,
                Module = module,
                Path = context.Request.Path.Value,
                HttpMethod = context.Request.Method,
                RequestId = context.TraceIdentifier,
                CorrelationId = context.TraceIdentifier,
                ActorUserId = actorUserId,
                ActorEmail = actorEmail,
                ActorDisplayName = actorDisplayName,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                QueryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : null,
                InnerExceptionType = exception.InnerException?.GetType().Name,
                InnerExceptionMessage = exception.InnerException?.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist ErrorLog record. RequestId: {RequestId}", context.TraceIdentifier);
        }
    }

    private static string ResolveModuleName(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Unknown";
        }

        var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : "Unknown";
    }
}


