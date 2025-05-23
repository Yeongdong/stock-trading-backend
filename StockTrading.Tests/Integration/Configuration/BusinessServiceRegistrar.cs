using Microsoft.Extensions.DependencyInjection;
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
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<StockTrading.DataAccess.Repositories.IUserRepository, UserRepository>();
        services.AddScoped<StockTrading.DataAccess.Repositories.IOrderRepository, OrderRepository>();
        services.AddScoped<StockTrading.DataAccess.Repositories.IKisTokenRepository, KisTokenRepository>();
        services.AddScoped<StockTrading.DataAccess.Repositories.IUserKisInfoRepository, UserKisInfoRepository>();
    }

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddScoped<StockTrading.DataAccess.Services.Interfaces.IJwtService, JwtService>();
        services.AddScoped<StockTrading.DataAccess.Services.Interfaces.IUserService, UserService>();
        services.AddScoped<StockTrading.DataAccess.Services.Interfaces.IGoogleAuthProvider, GoogleAuthProvider>();
        services.AddScoped<StockTrading.DataAccess.Services.Interfaces.IKisService, KisService>();
        services.AddScoped<StockTrading.DataAccess.Services.Interfaces.IKisTokenService, KisTokenService>();
    }

    private static void RegisterInfrastructureServices(IServiceCollection services)
    {
        services.AddScoped<StockTrading.Infrastructure.ExternalServices.Interfaces.IKisApiClient, KisApiClient>();
        services.AddScoped<IDbContextWrapper, DbContextWrapper>();
    }
}