using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;
using StockTrading.Infrastructure.Security.Encryption;
using StockTrading.Infrastructure.Security.Options;
using StockTrading.API.Middleware;
using StockTrading.API.Services;
using StockTrading.API.Validator.Implementations;
using StockTrading.API.Validator.Interfaces;
using StockTrading.Application.Repositories;
using StockTrading.Application.Services;
using StockTrading.Domain.Settings;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Converters;
using StockTrading.Infrastructure.Persistence.Contexts;
using StockTrading.Infrastructure.Persistence.Repositories;
using StockTrading.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 중요: EUC-KR 인코딩 지원(KRX 종목 조회용)
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// 로깅 설정 개선
ConfigureLogging(builder);

// 1. 기본 서비스 등록
ConfigureBasicServices(builder.Services);

// 2. 데이터베이스 설정
ConfigureDatabase(builder.Services, builder.Configuration);

// 3. 보안 서비스 설정
ConfigureSecurity(builder.Services, builder.Configuration);

// 4. 인증 설정
ConfigureAuthentication(builder.Services, builder.Configuration);

// 5. 외부 API 클라이언트 설정
ConfigureHttpClients(builder.Services, builder.Configuration);

// 6. 비즈니스 서비스 등록
ConfigureBusinessServices(builder.Services, builder.Configuration);

// 7. 실시간 서비스 등록
ConfigureRealTimeServices(builder.Services);

// 8. CORS 설정
ConfigureCors(builder.Services, builder.Configuration);

// 9. 헬스체크 추가
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

// 미들웨어 파이프라인 구성
ConfigureMiddleware(app);

app.Run();

#region 서비스 설정 메서드들

static void ConfigureLogging(WebApplicationBuilder builder)
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
}

static void ConfigureBasicServices(IServiceCollection services)
{
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();
    services.AddControllers();
    services.AddHttpContextAccessor();
    services.AddSignalR();
    services.AddMemoryCache();
}

static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

        // 개발 환경에서만 민감한 데이터 로깅 활성화
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            options.EnableSensitiveDataLogging();
        }
    });

    services.AddScoped<ApplicationDbContext>(provider =>
    {
        var options = provider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
        var encryptionService = provider.GetRequiredService<IEncryptionService>();
        return new ApplicationDbContext(options, encryptionService);
    });
}

static void ConfigureSecurity(IServiceCollection services, IConfiguration configuration)
{
    // JWT 설정 등록
    services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

    // 암호화 서비스 등록
    // 환경변수가 있으면 우선 사용
    services.Configure<EncryptionOptions>(options =>
    {
        var config = configuration.GetSection("Encryption");
        options.Key = Environment.GetEnvironmentVariable("ENCRYPTION:KEY") ?? config["Key"];
        options.IV = Environment.GetEnvironmentVariable("ENCRYPTION:IV") ?? config["IV"];
    });
    services.AddSingleton<IEncryptionService, AesEncryptionService>();
}

static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
{
    var jwtSettings = configuration.GetSection("JwtSettings");
    var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

    services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // 쿠키에서 토큰 읽기
                    if (context.Request.Cookies.TryGetValue("auth_token", out var token))
                    {
                        context.Token = token;
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogDebug("🍪 [JWT] 쿠키에서 토큰 추출: {HasToken}", !string.IsNullOrEmpty(token));
                    }
                    
                    // SignalR 연결을 위한 쿼리 스트링에서 토큰 읽기
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/stockhub"))
                    {
                        context.Token = accessToken;
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogDebug("🔗 [JWT] SignalR 쿼리에서 토큰 추출: {HasToken}", !string.IsNullOrEmpty(accessToken));
                    }
                    
                    return Task.CompletedTask;;
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var path = context.Request.Path;
                    
                    if (path.StartsWithSegments("/stockhub"))
                    {
                        logger.LogWarning("🚫 [JWT] SignalR 인증 실패: {Error} | Path: {Path}", 
                            context.Exception.Message, path);
                    }
                    else
                    {
                        logger.LogWarning("🚫 [JWT] 인증 실패: {Error} | Path: {Path}", 
                            context.Exception.Message, path);
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                    var path = context.Request.Path;
                    
                    if (path.StartsWithSegments("/stockhub"))
                    {
                        logger.LogDebug("✅ [JWT] SignalR 토큰 검증 성공: {Email}", email);
                    }
                    else
                    {
                        logger.LogDebug("✅ [JWT] 토큰 검증 성공: {Email}", email);
                    }
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var path = context.Request.Path;
                    
                    if (path.StartsWithSegments("/stockhub"))
                    {
                        logger.LogWarning("🔐 [JWT] SignalR 인증 요구됨: {Path}", path);
                    }
                    else
                    {
                        logger.LogWarning("🔐 [JWT] 인증 요구됨: {Path}", path);
                    }
                    return Task.CompletedTask;

                }
            };
        })
        .AddGoogle("Google", options =>
        {
            options.ClientId = configuration["Authentication:Google:ClientId"];
            options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
            options.CallbackPath = "/api/auth/oauth2/callback/google";
        });
}

static void ConfigureHttpClients(IServiceCollection services, IConfiguration configuration)
{
    var kisBaseUrl = configuration["KoreaInvestment:BaseUrl"];
    var krxBaseUrl = configuration["KrxApi:BaseUrl"];

    services.AddHttpClient();

    services.AddHttpClient<KisApiClient>(client =>
    {
        client.BaseAddress = new Uri(kisBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "StockTradingApp/1.0");
    });

    services.AddHttpClient<OrderService>(client =>
    {
        client.BaseAddress = new Uri(kisBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "StockTradingApp/1.0");
    });
    
    services.AddHttpClient(nameof(KisTokenService), client =>
    {
        client.BaseAddress = new Uri(kisBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "StockTradingApp/1.0");
    });
    
    services.AddHttpClient<KrxApiClient>(client =>
    {
        client.BaseAddress = new Uri(krxBaseUrl);
    });

    services.AddScoped<IKisApiClient>(provider => provider.GetRequiredService<KisApiClient>());
}

static void ConfigureBusinessServices(IServiceCollection services, IConfiguration configuration)
{
    // Repository 계층
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IOrderRepository, OrderRepository>();
    services.AddScoped<ITokenRepository, TokenRepository>();
    services.AddScoped<IUserKisInfoRepository, UserKisInfoRepository>();
    services.AddScoped<IStockRepository, StockRepository>();

    // Infrastructure 계층
    services.AddScoped<IDbContextWrapper, DbContextWrapper>();

    // Application 서비스 계층
    services.AddScoped<IJwtService, JwtService>();
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<IOrderService, OrderService>();
    services.AddScoped<IBalanceService, BalanceService>();
    services.AddScoped<IStockService, StockService>();
    services.AddScoped<IKisTokenService, KisTokenService>();
    services.AddScoped<IOrderExecutionInquiryService, OrderExecutionInquiryService>();
    services.AddScoped<IBuyableInquiryService, BuyableInquiryService>();
    services.AddScoped<ICurrentPriceService, CurrentPriceService>(); 

    // API 계층
    services.AddScoped<IUserContextService, UserContextService>();

    // Validator 계층
    services.AddScoped<IGoogleAuthValidator, GoogleAuthValidator>();

    // 설정 등록
    services.Configure<KisApiSettings>(configuration.GetSection(KisApiSettings.SectionName));
    services.Configure<KrxApiSettings>(configuration.GetSection(KrxApiSettings.SectionName));
    
    // Converter 등록
    services.AddScoped<StockDataConverter>();
}

static void ConfigureRealTimeServices(IServiceCollection services)
{
    services.AddSingleton<WebSocketClient>();
    services.AddSingleton<IWebSocketClient>(provider => provider.GetRequiredService<WebSocketClient>());
    
    services.AddSingleton<RealTimeDataBroadcaster>(provider =>
    {
        var hubContext = provider.GetRequiredService<IHubContext<StockHub>>();
        var logger = provider.GetRequiredService<ILogger<RealTimeDataBroadcaster>>();
        var broadcaster = new RealTimeDataBroadcaster(hubContext, logger);
        
        logger.LogInformation("🔧 [DI] RealTimeDataBroadcaster 인스턴스 생성됨");
        return broadcaster;
    });
    services.AddSingleton<IRealTimeDataBroadcaster>(provider => provider.GetRequiredService<RealTimeDataBroadcaster>());
    
    services.AddSingleton<RealTimeDataProcessor>(provider =>
    {
        var logger = provider.GetRequiredService<ILogger<RealTimeDataProcessor>>();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var broadcaster = provider.GetRequiredService<IRealTimeDataBroadcaster>();
        
        var processor = new RealTimeDataProcessor(logger, loggerFactory);
        
        logger.LogInformation("🔧 [DI] RealTimeDataProcessor 이벤트 핸들러 연결 시작");
        
        processor.StockPriceReceived += async (sender, data) =>
        {
            logger.LogInformation("🎯 [DI] StockPriceReceived 이벤트 발생: {Symbol}", data.Symbol);
            await broadcaster.BroadcastStockPriceAsync(data);
        };
        
        processor.TradeExecutionReceived += async (sender, data) =>
        {
            logger.LogInformation("🎯 [DI] TradeExecutionReceived 이벤트 발생");
            await broadcaster.BroadcastTradeExecutionAsync(data);
        };
        
        logger.LogInformation("✅ [DI] RealTimeDataProcessor 이벤트 핸들러 연결 완료");
        return processor;
    });
    services.AddSingleton<IRealTimeDataProcessor>(provider => provider.GetRequiredService<RealTimeDataProcessor>());
    
    services.AddSingleton<SubscriptionManager>();
    services.AddSingleton<ISubscriptionManager>(provider => provider.GetRequiredService<SubscriptionManager>());
    
    services.AddSingleton<IRealTimeService, RealTimeService>();
}

static void ConfigureCors(IServiceCollection services, IConfiguration configuration)
{
    var frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:3000";

    services.AddCors(options =>
    {
        options.AddPolicy("AllowReactApp", builder =>
        {
            builder.WithOrigins(frontendUrl)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .SetIsOriginAllowedToAllowWildcardSubdomains();
        });

        // 개발 환경용 정책
        options.AddPolicy("Development", builder =>
        {
            builder.WithOrigins(frontendUrl, "http://localhost:3000", "https://localhost:3000")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .SetPreflightMaxAge(TimeSpan.FromSeconds(86400))
                .WithExposedHeaders("Connection", "Upgrade")
                .SetIsOriginAllowed(_ => true);
        });
    });
}

static void ConfigureMiddleware(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    // 1. 전역 예외 처리 (가장 먼저)
    app.UseMiddleware<ExceptionMiddleware>();

    // 2. 개발 환경별 설정
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Stock Trading API V1");
            c.RoutePrefix = "swagger";
        });

        // 개발 환경 요청 로깅
        app.Use(async (context, next) =>
        {
            var startTime = DateTime.UtcNow;
            // SignalR 연결은 로깅에서 제외 (너무 많은 로그 방지)
            if (!context.Request.Path.StartsWithSegments("/stockhub"))
            {
                await next();
                var duration = DateTime.UtcNow - startTime;

                logger.LogDebug("HTTP {Method} {Path} - {StatusCode} ({Duration}ms)",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    duration.TotalMilliseconds);
            }
            else
            {
                await next();
            }
        });

        logger.LogInformation("개발 환경: Swagger UI 및 상세 로깅 활성화됨");
    }
    else
    {
        // 운영 환경 보안 설정
        app.UseHsts();
        app.Use(async (context, next) =>
        {
            // 보안 헤더 추가
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
        logger.LogInformation("운영 환경: 보안 헤더 적용됨");
    }

    // SignalR 연결 로깅 (일반 HTTP 요청과 분리)
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/stockhub"))
        {
            logger.LogInformation("🔗 [SignalR] 연결 요청: {Method} {Path} | User-Agent: {UserAgent} | Origin: {Origin}",
                context.Request.Method,
                context.Request.Path,
                context.Request.Headers.UserAgent.ToString(),
                context.Request.Headers.Origin.ToString());
        }
        else
        {
            logger.LogInformation("Request: {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }
        
        await next();

        logger.LogInformation(
            context.Request.Path.StartsWithSegments("/stockhub")
                ? "📡 [SignalR] 응답: {StatusCode}"
                : "Response: {StatusCode}", context.Response.StatusCode);
    });

    
    // app.Use(async (context, next) =>
    // {
    //     logger.LogInformation("Request: {Method} {Path}",
    //         context.Request.Method,
    //         context.Request.Path);
    //     await next();
    //     logger.LogInformation("Response: {StatusCode}", context.Response.StatusCode);
    // });


    // 3. CORS (인증 전에 위치해야 함)
    if (app.Environment.IsDevelopment())
    {
        app.UseCors("Development");
    }
    else
    {
        app.UseCors("AllowReactApp");
    }

    // 4. HTTPS 리다이렉션
    app.UseHttpsRedirection();

    // 5. 라우팅
    app.UseRouting();

    // 6. 인증 (Authorization 전에 위치)
    app.UseAuthentication();

    // 7. 인가
    app.UseAuthorization();

    // 8. 엔드포인트 매핑
    app.MapControllers();
    // app.MapHub<StockHub>("/stockhub");
    app.MapHub<StockHub>("/stockhub", options =>
    {
        logger.LogInformation("📡 [SignalR] Hub 매핑 완료: /stockhub");
        
        // SignalR 전송 옵션 설정
        options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets | 
                             Microsoft.AspNetCore.Http.Connections.HttpTransportType.ServerSentEvents;
        
        // 타임아웃 설정
        options.CloseOnAuthenticationExpiration = false;
        
        // 개발 환경에서 더 관대한 설정
        if (app.Environment.IsDevelopment())
        {
            options.ApplicationMaxBufferSize = 64 * 1024; // 64KB
            options.TransportMaxBufferSize = 64 * 1024;   // 64KB
        }
    });
    
    // 9. 헬스체크 엔드포인트
    app.MapHealthChecks("/health");
    
    // 10. SignalR 테스트 엔드포인트 (개발 환경에서만)
    if (app.Environment.IsDevelopment())
    {
        app.MapGet("/signalr-test", () => Results.Ok(new
        {
            message = "SignalR Hub is available at /stockhub",
            hubUrl = "/stockhub",
            timestamp = DateTime.UtcNow
        }));
    }

    // 10. 기본 상태 확인 엔드포인트
    app.MapGet("/", () => Results.Ok(new
    {
        message = "Stock Trading API is running",
        version = "1.0.0",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    }));

    logger.LogInformation("미들웨어 파이프라인 구성 완료");
}

#endregion

public partial class Program
{
}