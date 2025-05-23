using Microsoft.Extensions.DependencyInjection;
using stock_trading_backend.Validator.Implementations;
using stock_trading_backend.Validator.Interfaces;
using StockTrading.DataAccess.Repositories;
using StockTrading.DataAccess.Services.Interfaces;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;
using StockTrading.Infrastructure.Implementations;
using StockTrading.Infrastructure.Interfaces;
using StockTrading.Infrastructure.Repositories;

namespace StockTrading.Tests.Integration.Configuration;

/// <summary>
/// 비즈니스 서비스 등록 담당
/// </summary>
public static class BusinessServiceRegistrar
{
    public static void RegisterAllServices(IServiceCollection services)
    {
        RegisterRepositories(services);
        RegisterApplicationServices(services);
        RegisterInfrastructureServices(services);
        RegisterValidatorServices(services);
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IKisTokenRepository, KisTokenRepository>();
        services.AddScoped<IUserKisInfoRepository, UserKisInfoRepository>();
    }

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IGoogleAuthProvider, GoogleAuthProvider>();
        services.AddScoped<IKisService, KisService>();
        services.AddScoped<IKisTokenService, KisTokenService>();
    }

    private static void RegisterInfrastructureServices(IServiceCollection services)
    {
        services.AddScoped<StockTrading.Infrastructure.ExternalServices.Interfaces.IKisApiClient, KisApiClient>();
        services.AddScoped<IDbContextWrapper, DbContextWrapper>();
    }
    
    /// <summary>
    /// 검증(Validator) 서비스 등록
    /// </summary>
    private static void RegisterValidatorServices(IServiceCollection services)
    {
        if (services.All(s => s.ServiceType != typeof(IGoogleAuthValidator))) 
            services.AddScoped<IGoogleAuthValidator, GoogleAuthValidator>();
    }
}