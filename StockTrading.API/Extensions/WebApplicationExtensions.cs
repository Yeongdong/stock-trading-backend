using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using StockTrading.API.Middleware;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

namespace StockTrading.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureMiddlewarePipeline(this WebApplication app)
    {
        // 1. CORS 설정
        app.UseCors(app.Environment.IsDevelopment() ? "Development" : "AllowReactApp");
        
        // 2. 전역 예외 처리
        app.UseMiddleware<ExceptionMiddleware>();

        // 3. 환경별 설정
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
            app.UseSecurityHeaders();
        }
    
        // 4. 기본 미들웨어들
        app.UseHttpsRedirection();
        app.UseRouting();

        // 5. 인증 및 인가
        app.UseAuthentication();
        app.UseAuthorization();
    
        // 6. 엔드포인트 매핑
        app.MapControllers();
        app.MapHub<StockHub>("/stockhub");
        
        // Health Check 엔드포인트들
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready") || check.Name == "self",
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Name == "self",
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // 7. 기본 상태 확인
        app.MapGet("/", () => Results.Ok(new
        {
            message = "Stock Trading API is running",
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            environment = app.Environment.EnvironmentName
        }));

        return app;
    }

    private static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/stockhub"))
            {
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            }

            await next();
        });

        return app;
    }
}