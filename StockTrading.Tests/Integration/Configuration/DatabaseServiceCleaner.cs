using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockTrading.Infrastructure.Repositories;

namespace StockTrading.Tests.Integration.Configuration;

/// <summary>
/// 데이터베이스 서비스 정리 담당
/// </summary>
public static class DatabaseServiceCleaner
{
    public static void RemoveExistingDbContextServices(IServiceCollection services)
    {
        var descriptorsToRemove = services
            .Where(d => d.ServiceType == typeof(ApplicationDbContext) ||
                        d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                        d.ServiceType.Name.Contains("DbContext") ||
                        (d.ImplementationType != null &&
                         (d.ImplementationType == typeof(ApplicationDbContext) ||
                          d.ImplementationType.Name.Contains("ApplicationDbContext"))))
            .ToList();

        foreach (var descriptor in descriptorsToRemove)
        {
            services.Remove(descriptor);
        }
    }
}