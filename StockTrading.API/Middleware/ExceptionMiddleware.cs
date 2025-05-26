using System.Net;
using System.Text.Json;

namespace StockTrading.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message) = ex switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
            ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
            InvalidOperationException => (HttpStatusCode.InternalServerError, ex.Message),
            _ => (HttpStatusCode.InternalServerError, ex.Message)
        };

        _logger.LogError(ex, "API Error: {Message}", ex.Message);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new { message };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}