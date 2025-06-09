using StockTrading.API.Middleware;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

namespace StockTrading.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureMiddlewarePipeline(this WebApplication app)
    {
        // 1. 전역 예외 처리
        app.UseMiddleware<ExceptionMiddleware>();

        // 2. 환경별 설정
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

        // 3. 기본 미들웨어들
        app.UseHttpsRedirection();
        app.UseRouting();

        // 4. CORS
        app.UseCors(app.Environment.IsDevelopment() ? "Development" : "AllowReactApp");

        // 5. 인증 및 인가
        app.UseAuthentication();
        app.UseAuthorization();

        // 6. 엔드포인트 매핑
        app.MapControllers();
        app.MapHub<StockHub>("/stockhub");
        app.MapHealthChecks("/health");

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