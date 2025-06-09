using StockTrading.API.Extensions;

namespace StockTrading.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddApplicationServices(builder.Configuration);

        var app = builder.Build();

        app.ConfigureMiddlewarePipeline();

        app.Run();
    }
}