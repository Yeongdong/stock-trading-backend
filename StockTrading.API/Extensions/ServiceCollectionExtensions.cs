using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StockTrading.API.Services;
using StockTrading.API.Validator.Implementations;
using StockTrading.API.Validator.Interfaces;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Repositories;
using StockTrading.Application.Services;
using StockTrading.Domain.Settings;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Converters;
using StockTrading.Infrastructure.ExternalServices.KRX;
using StockTrading.Infrastructure.Persistence.Contexts;
using StockTrading.Infrastructure.Persistence.Repositories;
using StockTrading.Infrastructure.Security.Encryption;
using StockTrading.Infrastructure.Security.Options;
using StockTrading.Infrastructure.Services;

namespace StockTrading.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBasicServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddControllers();
        services.AddHttpContextAccessor();
        services.AddSignalR();
        services.AddMemoryCache();

        return services;
    }

    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
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

        // ApplicationDbContext를 수동으로 등록 (IEncryptionService 의존성 때문에)
        services.AddScoped<ApplicationDbContext>(provider =>
        {
            var options = provider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
            var encryptionService = provider.GetRequiredService<IEncryptionService>();
            return new ApplicationDbContext(options, encryptionService);
        });

        return services;
    }

    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // JWT 설정 등록
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        // 암호화 서비스 등록 (환경변수 우선)
        services.Configure<EncryptionOptions>(options =>
        {
            var config = configuration.GetSection("Encryption");
            options.Key = Environment.GetEnvironmentVariable("ENCRYPTION:KEY") ?? config["Key"];
            options.IV = Environment.GetEnvironmentVariable("ENCRYPTION:IV") ?? config["IV"];
        });

        services.AddSingleton<IEncryptionService, AesEncryptionService>();

        return services;
    }

    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services,
        IConfiguration configuration)
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

                options.Events = CreateJwtBearerEvents();
            })
            .AddGoogle("Google", options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"];
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
                options.CallbackPath = "/api/auth/oauth2/callback/google";
            });

        return services;
    }

    public static IServiceCollection AddHttpClientServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var kisBaseUrl = configuration["KoreaInvestment:BaseUrl"];
        var krxBaseUrl = configuration["KrxApi:BaseUrl"];

        services.AddHttpClient();

        AddKisHttpClient<KisOrderApiClient>(services, kisBaseUrl);
        AddKisHttpClient<KisBalanceApiClient>(services, kisBaseUrl);
        AddKisHttpClient<KisPriceApiClient>(services, kisBaseUrl);

        services.AddHttpClient<OrderService>(client => ConfigureKisHttpClient(client, kisBaseUrl));
        services.AddHttpClient(nameof(KisTokenService), client => ConfigureKisHttpClient(client, kisBaseUrl));
        services.AddHttpClient<KrxApiClient>(client => { client.BaseAddress = new Uri(krxBaseUrl); });

        return services;
    }

    private static void AddKisHttpClient<T>(IServiceCollection services, string baseUrl) where T : class
    {
        services.AddHttpClient<T>(client => ConfigureKisHttpClient(client, baseUrl));
    }

    private static void ConfigureKisHttpClient(HttpClient client, string? kisBaseUrl)
    {
        client.BaseAddress = new Uri(kisBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "StockTradingApp/1.0");
    }

    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        AddRepositories(services);
        AddInfrastructureServices(services);
        AddApplicationServices(services);
        AddApiServices(services);
        AddValidators(services);
        AddConvertersAndSettings(services, configuration);

        return services;
    }

    public static IServiceCollection AddRealTimeServices(this IServiceCollection services)
    {
        services.AddSingleton<WebSocketClient>();
        services.AddSingleton<IWebSocketClient>(provider => provider.GetRequiredService<WebSocketClient>());

        services.AddSingleton<RealTimeDataBroadcaster>(provider =>
        {
            var hubContext = provider.GetRequiredService<IHubContext<StockHub>>();
            var logger = provider.GetRequiredService<ILogger<RealTimeDataBroadcaster>>();
            var broadcaster = new RealTimeDataBroadcaster(hubContext);

            logger.LogInformation("[DI] RealTimeDataBroadcaster 인스턴스 생성됨");
            return broadcaster;
        });
        services.AddSingleton<IRealTimeDataBroadcaster>(provider =>
            provider.GetRequiredService<RealTimeDataBroadcaster>());

        services.AddSingleton<RealTimeDataProcessor>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<RealTimeDataProcessor>>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var settings = provider.GetRequiredService<IOptions<RealTimeDataSettings>>();
            var converter = provider.GetRequiredService<StockDataConverter>();
            var broadcaster = provider.GetRequiredService<IRealTimeDataBroadcaster>();

            var processor = new RealTimeDataProcessor(logger, loggerFactory, settings, converter);

            logger.LogInformation("[DI] RealTimeDataProcessor 이벤트 핸들러 연결 시작");

            processor.StockPriceReceived += async (sender, data) =>
            {
                logger.LogInformation("[DI] StockPriceReceived 이벤트 발생: {Symbol}", data.Symbol);
                await broadcaster.BroadcastStockPriceAsync(data);
            };

            processor.TradeExecutionReceived += async (sender, data) =>
            {
                logger.LogInformation("[DI] TradeExecutionReceived 이벤트 발생");
                await broadcaster.BroadcastTradeExecutionAsync(data);
            };

            logger.LogInformation("[DI] RealTimeDataProcessor 이벤트 핸들러 연결 완료");
            return processor;
        });
        services.AddSingleton<IRealTimeDataProcessor>(provider => provider.GetRequiredService<RealTimeDataProcessor>());

        services.AddSingleton<SubscriptionManager>();
        services.AddSingleton<ISubscriptionManager>(provider => provider.GetRequiredService<SubscriptionManager>());

        services.AddSingleton<IRealTimeService, RealTimeService>();

        return services;
    }

    public static IServiceCollection AddCorsServices(this IServiceCollection services, IConfiguration configuration)
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

        return services;
    }

    public static void AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>();
    }

    private static void AddConvertersAndSettings(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSettingsWithValidation(configuration);
        services.ValidateAllSettingsOnStartup();
        services.AddSettingsSummary();

        services.AddScoped<StockDataConverter>();
    }

    private static void AddValidators(IServiceCollection services)
    {
        services.AddScoped<IGoogleAuthValidator, GoogleAuthValidator>();
    }

    private static void AddApiServices(IServiceCollection services)
    {
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IKisOrderApiClient>(provider => provider.GetRequiredService<KisOrderApiClient>());
        services.AddScoped<IKisBalanceApiClient>(provider => provider.GetRequiredService<KisBalanceApiClient>());
        services.AddScoped<IKisPriceApiClient>(provider => provider.GetRequiredService<KisPriceApiClient>());
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IBalanceService, BalanceService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IKisTokenService, KisTokenService>();
        services.AddScoped<IOrderExecutionInquiryService, OrderExecutionInquiryService>();
        services.AddScoped<IBuyableInquiryService, BuyableInquiryService>();
        services.AddScoped<ICurrentPriceService, CurrentPriceService>();
    }

    private static void AddInfrastructureServices(IServiceCollection services)
    {
        services.AddScoped<IDbContextWrapper, DbContextWrapper>();
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ITokenRepository, TokenRepository>();
        services.AddScoped<IUserKisInfoRepository, UserKisInfoRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
    }

    #region Private Helper Methods

    private static JwtBearerEvents CreateJwtBearerEvents()
    {
        return new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("auth_token", out var token))
                {
                    context.Token = token;
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[JWT] 쿠키에서 토큰 추출: {HasToken}", !string.IsNullOrEmpty(token));
                }

                // SignalR 연결을 위한 쿼리 스트링에서 토큰 읽기
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (string.IsNullOrEmpty(accessToken) || !path.StartsWithSegments("/stockhub"))
                    return Task.CompletedTask;
                {
                    context.Token = accessToken;
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("[JWT] SignalR 쿼리에서 토큰 추출: {HasToken}", !string.IsNullOrEmpty(accessToken));
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var path = context.Request.Path;

                logger.LogWarning(
                    path.StartsWithSegments("/stockhub")
                        ? "[JWT] SignalR 인증 실패: {Error} | Path: {Path}"
                        : "[JWT] 인증 실패: {Error} | Path: {Path}",
                    context.Exception.Message, path);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var path = context.Request.Path;

                logger.LogDebug(
                    path.StartsWithSegments("/stockhub")
                        ? "[JWT] SignalR 토큰 검증 성공: {Email}"
                        : "[JWT] 토큰 검증 성공: {Email}", email);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var path = context.Request.Path;

                logger.LogWarning(
                    path.StartsWithSegments("/stockhub")
                        ? "[JWT] SignalR 인증 요구됨: {Path}"
                        : "[JWT] 인증 요구됨: {Path}", path);
                return Task.CompletedTask;
            }
        };
    }

    #endregion
}