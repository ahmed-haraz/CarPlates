using System.Net;
using System.Text.Json;

namespace CarPlates.API.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment environment)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionMiddleware> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // Exception messages can contain internals (SQL text, file paths, stack
        // details) that shouldn't leave the server. Only Development builds get
        // the detailed message; every other environment gets a generic one.
        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "An internal server error occurred.",
            Detailed = _environment.IsDevelopment() ? exception.Message : null
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
