using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Stock;
using StockTrading.Application.Repositories;
using StockTrading.Application.Services;
using StockTrading.Domain.Entities;
using StockTrading.Domain.Enums;
using StockTrading.Domain.Extensions;
using StockTrading.Infrastructure.ExternalServices.KRX;

namespace StockTrading.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly IStockRepository _stockRepository;
    private readonly KrxApiClient _krxApiClient;
    private readonly IStockCacheService _stockCacheService;
    private readonly ILogger<StockService> _logger;

    public StockService(IStockRepository stockRepository, KrxApiClient krxApiClient,
        IStockCacheService stockCacheService, ILogger<StockService> logger)
    {
        _stockRepository = stockRepository;
        _krxApiClient = krxApiClient;
        _stockCacheService = stockCacheService;
        _logger = logger;
    }

    public async Task<List<StockSearchResult>> SearchStocksAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        var cachedResult = await _stockCacheService.GetSearchResultAsync(searchTerm, page, pageSize);
        if (cachedResult != null) return cachedResult.Stocks;

        var stocks = await _stockRepository.SearchByNameAsync(searchTerm, page, pageSize);

        var results = stocks.Select(stock => new StockSearchResult
        {
            Code = stock.Code,
            Name = stock.Name,
            EnglishName = stock.EnglishName,
            Sector = stock.Sector,
            Market = stock.Market.GetDescription()
        }).ToList();

        var totalCount = await GetSearchTotalCountAsync(searchTerm);
        await _stockCacheService.SetSearchResultAsync(searchTerm, page, pageSize, results, totalCount);

        return results;
    }

    public async Task<StockSearchResult?> GetStockByCodeAsync(string code)
    {
        var cachedResult = await _stockCacheService.GetStockByCodeAsync(code);
        if (cachedResult != null) return cachedResult;

        var stock = await _stockRepository.GetByCodeAsync(code);

        if (stock == null) return null;
        var result = new StockSearchResult
        {
            Code = stock.Code,
            Name = stock.Name,
            EnglishName = stock.EnglishName,
            Sector = stock.Sector,
            Market = stock.Market.GetDescription()
        };
        await _stockCacheService.SetStockByCodeAsync(code, result);

        return result;
    }

    public async Task UpdateStockDataFromKrxAsync()
    {
        _logger.LogInformation("KRX 데이터 업데이트 시작");

        await _stockCacheService.InvalidateAllStockCacheAsync();
        var krxResponse = await _krxApiClient.GetStockListAsync();
        
        var validStocks = krxResponse.Stocks
            .Where(item => item.IsValid())
            .Select(item => new Stock(
                code: item.Code,
                name: item.Name,
                fullName: item.FullName,
                sector: NormalizeSector(item.Sector),
                market: NormalizeMarketName(item.SecurityGroup),
                englishName: item.EnglishName,
                stockType: ExtractStockType(item.StockTypedShares),
                parValue: null,
                listedShares: ExtractListedShares(item.StockTypedShares),
                listedDate: ParseListedDate(item.ListedDate)))
            .ToList();

        await _stockRepository.BulkUpsertAsync(validStocks);

        var summary = await GetSearchSummaryAsync();
        await _stockCacheService.SetStockSummaryAsync(summary);
    }

    public async Task<StockSearchSummary> GetSearchSummaryAsync()
    {
        var cachedSummary = await _stockCacheService.GetStockSummaryAsync();
        if (cachedSummary != null)
            return new StockSearchSummary
            {
                TotalCount = cachedSummary.TotalCount,
                LastUpdated = cachedSummary.LastUpdated,
                MarketCounts = cachedSummary.MarketCounts
            };

        var totalCount = await _stockRepository.GetTotalCountAsync();
        var lastUpdated = await _stockRepository.GetLastUpdatedAsync();

        var marketCounts = new Dictionary<string, int>();
        foreach (var market in Enum.GetValues<Market>())
        {
            var stocks = await _stockRepository.GetByMarketAsync(market);
            marketCounts[market.GetDescription()] = stocks.Count;
        }

        var summary = new StockSearchSummary
        {
            TotalCount = totalCount,
            LastUpdated = lastUpdated,
            MarketCounts = marketCounts
        };

        await _stockCacheService.SetStockSummaryAsync(summary);

        return summary;
    }

    #region Private Helper Methods

    private async Task<int> GetSearchTotalCountAsync(string searchTerm)
    {
        var allResults = await _stockRepository.SearchByNameAsync(searchTerm, 1, int.MaxValue);
        return allResults.Count;
    }

    private static string NormalizeSector(string? sector)
    {
        if (string.IsNullOrWhiteSpace(sector)) return "기타";

        var normalizedSector = sector.Trim();
        return string.IsNullOrEmpty(normalizedSector) ? "기타" : normalizedSector;
    }

    private static string? ExtractStockType(string? stockTypedShares)
    {
        return string.IsNullOrWhiteSpace(stockTypedShares) ? null : "보통주";
    }


    private static string? ExtractListedShares(string? stockTypedShares)
    {
        if (string.IsNullOrWhiteSpace(stockTypedShares)) return null;

        var cleanedShares = stockTypedShares.Replace(",", "").Trim();
        return cleanedShares.All(char.IsDigit) ? cleanedShares : null;
    }

    private static DateTime? ParseListedDate(string? listedDateStr)
    {
        if (string.IsNullOrWhiteSpace(listedDateStr) || listedDateStr.Length != 8)
            return null;

        if (DateTime.TryParseExact(listedDateStr, "yyyy/MM/dd", null,
                System.Globalization.DateTimeStyles.None, out var date))
            return date;

        return null;
    }

    private static Market NormalizeMarketName(string? securityGroup)
    {
        if (string.IsNullOrEmpty(securityGroup)) return Market.Kospi;

        return securityGroup switch
        {
            _ when securityGroup.Contains("코스피") || securityGroup.Contains("KOSPI") => Market.Kospi,
            _ when securityGroup.Contains("코스닥") || securityGroup.Contains("KOSDAQ") => Market.Kosdaq,
            _ when securityGroup.Contains("코넥스") || securityGroup.Contains("KONEX") => Market.Konex,
            _ when securityGroup.Contains("주권") => Market.Kospi,
            _ => Market.Kospi
        };
    }

    #endregion
}