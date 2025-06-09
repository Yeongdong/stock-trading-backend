using Microsoft.Extensions.Options;
using StockTrading.Domain.Settings;
using StockTrading.Domain.Settings.Infrastructure;

namespace StockTrading.Infrastructure.Cache;

public class CacheTtl
{
    private readonly CacheSettings _settings;

    public CacheTtl(IOptions<CacheSettings> settings)
    {
        _settings = settings.Value;
    }
    
    public TimeSpan AllStocks => TimeSpan.FromHours(_settings.Ttl.AllStocksHours);
    public TimeSpan SearchResults => TimeSpan.FromHours(_settings.Ttl.SearchResultsHours);
    public TimeSpan StockDetail => TimeSpan.FromHours(_settings.Ttl.StockDetailHours);
    public TimeSpan AutoComplete => TimeSpan.FromHours(_settings.Ttl.AutoCompleteHours);
    public TimeSpan Metadata => TimeSpan.FromHours(_settings.Ttl.MetadataHours);
    
    public TimeSpan GetDynamicTtl(string cacheType, int accessCount = 0)
    {
        var baseTtl = cacheType switch
        {
            "search" => SearchResults,
            "autocomplete" => AutoComplete,
            "stock" => StockDetail,
            "metadata" => Metadata,
            _ => SearchResults
        };

        return accessCount switch
        {
            > 100 => baseTtl.Add(TimeSpan.FromHours(2)),
            > 50 => baseTtl.Add(TimeSpan.FromHours(1)),
            _ => baseTtl
        };
    }

}