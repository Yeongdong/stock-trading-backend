using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

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
            // 인증/인가 관련
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message),
            
            // 리소스 찾기 실패
            KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
            
            // 요청 데이터 검증 실패
            ArgumentNullException => (HttpStatusCode.BadRequest, ex.Message),
            ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
            
            // 데이터베이스 관련
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "동시성 충돌이 발생했습니다. 다시 시도해주세요."),
            DbUpdateException => (HttpStatusCode.Conflict, "데이터 저장 중 오류가 발생했습니다."),
            
            // 비즈니스 로직 오류
            InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message),
            
            // 외부 API 오류
            HttpRequestException => (HttpStatusCode.BadGateway, "외부 서비스 연결에 실패했습니다."),
            TaskCanceledException => (HttpStatusCode.RequestTimeout, "요청 시간이 초과되었습니다."),
            
            // 기타 서버 오류
            _ => (HttpStatusCode.InternalServerError, "서버에서 오류가 발생했습니다.")
        };

        _logger.LogError(ex, "API Error: {Message} | Path: {Path}", 
            ex.Message, context.Request.Path);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new { 
            message,
            traceId = context.TraceIdentifier // 디버깅용
        };
        
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}