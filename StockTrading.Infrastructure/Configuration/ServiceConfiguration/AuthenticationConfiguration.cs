using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using StockTrading.Application.Features.Auth.Services;
using StockTrading.Domain.Settings.Infrastructure;
using StockTrading.Infrastructure.Security.Encryption;
using StockTrading.Infrastructure.Security.Options;
using StockTrading.Infrastructure.Services.Auth;

namespace StockTrading.Infrastructure.Configuration.ServiceConfiguration;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                          ?? new JwtSettings();
        
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        
        AddEncryptionServices(services, configuration);
        AddJwtAuthentication(services, jwtSettings);
        AddGoogleAuthentication(services, configuration);

        return services;
    }

    /// <summary>
    /// 암호화 서비스 등록
    /// </summary>
    private static void AddEncryptionServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EncryptionOptions>(options =>
        {
            var config = configuration.GetSection("Encryption");
            // 환경변수 우선, 없으면 설정 파일에서
            options.Key = Environment.GetEnvironmentVariable("ENCRYPTION:KEY") ?? config["Key"];
            options.IV = Environment.GetEnvironmentVariable("ENCRYPTION:IV") ?? config["IV"];
        });

        services.AddSingleton<IEncryptionService, AesEncryptionService>();
    }

    private static void AddJwtAuthentication(IServiceCollection services, JwtSettings jwtSettings)
    {
        var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

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
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = CreateJwtBearerEvents();
            });
    }

    private static void AddGoogleAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication()
            .AddGoogle("Google", options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"];
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
                options.CallbackPath = "/api/auth/oauth2/callback/google";
            });
    }

    private static JwtBearerEvents CreateJwtBearerEvents()
    {
        return new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // 쿠키에서 토큰 추출
                if (context.Request.Cookies.TryGetValue("auth_token", out var token))
                    context.Token = token;

                // SignalR용 쿼리 스트링에서 토큰 추출
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/stockhub"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            }
        };
    }
}