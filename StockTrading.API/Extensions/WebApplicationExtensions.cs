using StockTrading.API.Middleware;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

namespace StockTrading.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigureMiddlewarePipeline(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        // 1. 전역 예외 처리 (가장 먼저)
        app.UseMiddleware<ExceptionMiddleware>();

        // 2. 환경별 설정
        app.ConfigureEnvironmentSpecificMiddleware();

        // 3. 요청 로깅
        app.ConfigureRequestLogging();

        // 4. CORS (인증 전에 위치)
        app.ConfigureCors();

        // 5. HTTPS 리다이렉션
        app.UseHttpsRedirection();

        // 6. 라우팅
        app.UseRouting();

        // 7. 인증 및 인가
        app.UseAuthentication();
        app.UseAuthorization();

        // 8. 엔드포인트 매핑
        app.ConfigureEndpoints();

        logger.LogInformation("미들웨어 파이프라인 구성 완료");
        return app;
    }

    public static WebApplication ConfigureEnvironmentSpecificMiddleware(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.ConfigureSwagger();
            logger.LogInformation("개발 환경: Swagger UI 및 개발자 예외 페이지 활성화됨");
        }
        else
        {
            app.ConfigureProductionSecurity();
            logger.LogInformation("운영 환경: 보안 헤더 적용됨");
        }

        return app;
    }

    public static WebApplication ConfigureSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Stock Trading API V1");
            c.RoutePrefix = "swagger";
        });

        return app;
    }

    public static WebApplication ConfigureProductionSecurity(this WebApplication app)
    {
        app.UseHsts();

        app.Use(async (context, next) =>
        {
            // SignalR 경로가 아닌 경우에만 보안 헤더 추가
            if (!context.Request.Path.StartsWithSegments("/stockhub"))
            {
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
                context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
            }

            await next();
        });

        return app;
    }

    public static WebApplication ConfigureRequestLogging(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.Use(async (context, next) =>
        {
            // SignalR 연결은 별도 처리
            if (context.Request.Path.StartsWithSegments("/stockhub"))
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("SignalR 요청: {Method} {Path}", context.Request.Method, context.Request.Path);
                await next();
                return;
            }

            var startTime = DateTime.UtcNow;
            await next();
            var duration = DateTime.UtcNow - startTime;

            // 오류나 느린 요청만 로깅
            if (context.Response.StatusCode >= 400 || duration.TotalMilliseconds > 1000)
            {
                app.Logger.LogWarning("HTTP {Method} {Path} - {StatusCode} ({Duration}ms)",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    duration.TotalMilliseconds);
            }
        });

        return app;
    }

    public static WebApplication ConfigureCors(this WebApplication app)
    {
        app.UseCors(app.Environment.IsDevelopment() ? "Development" : "AllowReactApp");

        return app;
    }

    public static WebApplication ConfigureEndpoints(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        // API 컨트롤러 매핑
        app.MapControllers();

        // SignalR Hub 매핑
        app.ConfigureSignalRHub();

        // 헬스체크 엔드포인트
        app.MapHealthChecks("/health");

        // 기본 상태 확인 엔드포인트
        app.ConfigureStatusEndpoints();

        return app;
    }

    public static WebApplication ConfigureSignalRHub(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        app.MapHub<StockHub>("/stockhub", options =>
        {
            logger.LogInformation("📡 [SignalR] Hub 매핑 완료: /stockhub");

            options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                                 Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents;

            options.CloseOnAuthenticationExpiration = false;

            // 개발 환경에서 더 관대한 설정
            if (!app.Environment.IsDevelopment()) return;
            options.ApplicationMaxBufferSize = 64 * 1024; // 64KB
            options.TransportMaxBufferSize = 64 * 1024; // 64KB
        });

        return app;
    }

    public static WebApplication ConfigureStatusEndpoints(this WebApplication app)
    {
        // 기본 상태 확인 엔드포인트
        app.MapGet("/", () => Results.Ok(new
        {
            message = "Stock Trading API is running",
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            environment = app.Environment.EnvironmentName
        }));

        // 개발 환경에서만 추가 디버그 엔드포인트
        if (app.Environment.IsDevelopment())
        {
            app.ConfigureDebugEndpoints();
        }

        return app;
    }

    public static WebApplication ConfigureDebugEndpoints(this WebApplication app)
    {
        // SignalR 테스트 엔드포인트
        app.MapGet("/signalr-test", () => Results.Ok(new
        {
            message = "SignalR Hub is available at /stockhub",
            hubUrl = "/stockhub",
            timestamp = DateTime.UtcNow
        }));

        // 환경 정보 엔드포인트
        app.MapGet("/debug/environment", () => Results.Ok(new
        {
            environment = app.Environment.EnvironmentName,
            machineName = Environment.MachineName,
            osVersion = Environment.OSVersion.ToString(),
            dotnetVersion = Environment.Version.ToString(),
            workingSet = Environment.WorkingSet,
            timestamp = DateTime.UtcNow
        }));

        // 설정 정보 엔드포인트 (민감한 정보 제외)
        app.MapGet("/debug/configuration", (IConfiguration configuration) => Results.Ok(new
        {
            connectionStringConfigured = !string.IsNullOrEmpty(configuration.GetConnectionString("DefaultConnection")),
            jwtSettingsConfigured = !string.IsNullOrEmpty(configuration["JwtSettings:Key"]),
            googleAuthConfigured = !string.IsNullOrEmpty(configuration["Authentication:Google:ClientId"]),
            kisApiConfigured = !string.IsNullOrEmpty(configuration["KoreaInvestment:BaseUrl"]),
            krxApiConfigured = !string.IsNullOrEmpty(configuration["KrxApi:BaseUrl"]),
            timestamp = DateTime.UtcNow
        }));

        return app;
    }

    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
        }
        else
        {
            builder.Logging.SetMinimumLevel(LogLevel.Information);
        }

        return builder;
    }
}