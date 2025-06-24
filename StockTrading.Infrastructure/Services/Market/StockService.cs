using Microsoft.Extensions.Logging;
using StockTrading.Application.Features.Market.DTOs.Stock;
using StockTrading.Application.Features.Market.Repositories;
using StockTrading.Application.Features.Market.Services;
using StockTrading.Domain.Enums;
using StockTrading.Domain.Extensions;
using StockTrading.Infrastructure.ExternalServices.KRX;

namespace StockTrading.Infrastructure.Services.Market;

public class StockService : IStockService
{
    private readonly IStockRepository _stockRepository;
    private readonly IStockCacheService _stockCacheService;
    private readonly KrxApiClient _krxApiClient;
    private readonly ILogger<StockService> _logger;

    public StockService(IStockRepository stockRepository, IStockCacheService stockCacheService,
        KrxApiClient krxApiClient, ILogger<StockService> logger)
    {
        _stockRepository = stockRepository;
        _stockCacheService = stockCacheService;
        _krxApiClient = krxApiClient;
        _logger = logger;
    }

    #region 주식 검색 및 조회

    public async Task<StockSearchResponse> SearchStocksAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        var cachedResult = await _stockCacheService.GetSearchResultAsync(searchTerm, page, pageSize);
        if (cachedResult != null)
            return cachedResult;

        var stocks = await _stockRepository.SearchByNameAsync(searchTerm, page, pageSize);
        var totalCount = await GetSearchTotalCountAsync(searchTerm);

        var stockResults = stocks.Select(s => new StockSearchResult
        {
            Code = s.Code,
            Name = s.Name,
            FullName = s.FullName,
            EnglishName = s.EnglishName,
            Sector = s.Sector,
            Market = s.Market.GetDescription(),
            Currency = s.Currency.GetDescription(),
            StockType = s.StockType,
            ListedDate = s.ListedDate,
            LastUpdated = s.LastUpdated
        }).ToList();

        var response = new StockSearchResponse
        {
            Results = stockResults,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            HasMore = (page * pageSize) < totalCount
        };

        await _stockCacheService.SetSearchResultAsync(searchTerm, page, pageSize, response);
        return response;
    }

    public async Task<StockSearchResult?> GetStockByCodeAsync(string code)
    {
        var cachedStock = await _stockCacheService.GetStockByCodeAsync(code);
        if (cachedStock != null)
            return cachedStock;

        var stock = await _stockRepository.GetByCodeAsync(code);
        if (stock == null)
            return null;

        var result = new StockSearchResult
        {
            Code = stock.Code,
            Name = stock.Name,
            FullName = stock.FullName,
            EnglishName = stock.EnglishName,
            Sector = stock.Sector,
            Market = stock.Market.GetDescription(),
            Currency = stock.Currency.GetDescription(),
            StockType = stock.StockType,
            ListedDate = stock.ListedDate,
            LastUpdated = stock.LastUpdated
        };

        await _stockCacheService.SetStockByCodeAsync(code, result);
        return result;
    }

    public async Task<List<StockSearchResult>> GetStocksByMarketAsync(StockTrading.Domain.Enums.Market market)
    {
        var stocks = await _stockRepository.GetByMarketAsync(market);

        return stocks.Select(s => new StockSearchResult
        {
            Code = s.Code,
            Name = s.Name,
            FullName = s.FullName,
            EnglishName = s.EnglishName,
            Sector = s.Sector,
            Market = s.Market.GetDescription(),
            Currency = s.Currency.GetDescription(),
            StockType = s.StockType,
            ListedDate = s.ListedDate,
            LastUpdated = s.LastUpdated
        }).ToList();
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
        foreach (var market in Enum.GetValues<StockTrading.Domain.Enums.Market>())
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

    #endregion

    #region 데이터 동기화

    public async Task SyncDomesticStockDataAsync()
    {
        _logger.LogInformation("국내 주식 데이터 동기화 시작");

        var stockListResponse = await _krxApiClient.GetStockListAsync();
        var validStocks = stockListResponse.Stocks
            .Where(item => !string.IsNullOrWhiteSpace(item.Code) && item.Code.Length == 6)
            .Select(item => new Domain.Entities.Stock(
                code: item.Code,
                name: item.Name,
                fullName: item.FullName,
                sector: NormalizeSector(item.Sector),
                market: NormalizeMarketName(item.SecurityGroup),
                currency: Currency.Krw,
                englishName: item.EnglishName,
                stockType: ExtractStockType(item.StockTypedShares),
                parValue: null,
                listedShares: ExtractListedShares(item.StockTypedShares),
                listedDate: ParseListedDate(item.ListedDate)))
            .ToList();

        await _stockRepository.BulkUpsertAsync(validStocks);

        var summary = await GetSearchSummaryAsync();
        await _stockCacheService.SetStockSummaryAsync(summary);

        _logger.LogInformation("국내 주식 데이터 동기화 완료: {Count}개", validStocks.Count);
    }

    #endregion

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

    private static StockTrading.Domain.Enums.Market NormalizeMarketName(string? securityGroup)
    {
        if (string.IsNullOrEmpty(securityGroup)) return StockTrading.Domain.Enums.Market.Kospi;

        return securityGroup switch
        {
            _ when securityGroup.Contains("코스피") || securityGroup.Contains("KOSPI") => StockTrading.Domain.Enums.Market
                .Kospi,
            _ when securityGroup.Contains("코스닥") || securityGroup.Contains("KOSDAQ") => StockTrading.Domain.Enums.Market
                .Kosdaq,
            _ when securityGroup.Contains("코넥스") || securityGroup.Contains("KONEX") => StockTrading.Domain.Enums.Market
                .Konex,
            _ when securityGroup.Contains("주권") => StockTrading.Domain.Enums.Market.Kospi,
            _ => StockTrading.Domain.Enums.Market.Kospi
        };
    }

    #endregion
}