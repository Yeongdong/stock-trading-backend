using StockTrading.API.Extensions;

namespace StockTrading.API;

public class Program
{
    public static void Main(string[] args)
    {
        // EUC-KR 인코딩 지원 (KRX API용)
        WebApplicationExtensions.RegisterEncodingProviders();

        var builder = WebApplication.CreateBuilder(args);

        // 로깅 설정
        builder.ConfigureLogging();

        // 서비스 등록
        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        // 미들웨어 파이프라인 구성
        app.ConfigureMiddlewarePipeline();

        // 애플리케이션 실행
        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddBasicServices()
            .AddDatabaseServices(configuration)
            .AddSecurityServices(configuration)
            .AddAuthenticationServices(configuration)
            .AddHttpClientServices(configuration)
            .AddBusinessServices(configuration)
            .AddRealTimeServices()
            .AddCorsServices(configuration)
            .AddHealthCheckServices();
    }
}