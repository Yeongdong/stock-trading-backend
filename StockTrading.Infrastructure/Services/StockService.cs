using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Stock;
using StockTrading.Application.Repositories;
using StockTrading.Application.Services;
using StockTrading.Domain.Entities;
using StockTrading.Domain.Enums;
using StockTrading.Domain.Extensions;

namespace StockTrading.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly IStockRepository _stockRepository;
    private readonly KrxApiClient _krxApiClient;
    private readonly ILogger<StockService> _logger;

    public StockService(IStockRepository stockRepository, KrxApiClient krxApiClient, ILogger<StockService> logger)
    {
        _stockRepository = stockRepository;
        _krxApiClient = krxApiClient;
        _logger = logger;
    }

    public async Task<List<StockSearchResult>> SearchStocksAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        _logger.LogInformation("종목 검색 시작: {SearchTerm}", searchTerm);

        var stocks = await _stockRepository.SearchByNameAsync(searchTerm, page, pageSize);

        _logger.LogInformation("종목 검색 완료: {Count}개 결과", stocks.Count);

        return stocks.Select(stock => new StockSearchResult
        {
            Code = stock.Code,
            Name = stock.Name,
            EnglishName = stock.EnglishName,
            Sector = stock.Sector,
            Market = stock.Market.GetDescription()
        }).ToList();
    }

    public async Task<Stock?> GetStockByCodeAsync(string code)
    {
        return await _stockRepository.GetByCodeAsync(code);
    }

    public async Task UpdateStockDataFromKrxAsync()
    {
        _logger.LogInformation("KRX 데이터 업데이트 시작");

        var krxResponse = await _krxApiClient.GetStockListAsync();

        var validStocks = krxResponse.Stocks
            .Where(item => item.IsValid()) // KrxStockItem의 IsValid() 메서드 사용
            .Select(item => new Stock(
                code: item.Code,
                name: item.Name,
                fullName: item.FullName,
                sector: item.Sector ?? "기타",
                market: NormalizeMarketName(item.SecurityGroup),
                englishName: item.EnglishName,
                stockType: item.StockType,
                parValue: item.ParValue,
                listedShares: item.ListedShares,
                listedDate: ParseListedDate(item.ListedDate)))
            .ToList();

        await _stockRepository.BulkUpsertAsync(validStocks);

        _logger.LogInformation("KRX 데이터 업데이트 완료: {Count}개 종목", validStocks.Count);
    }

    public async Task<StockSearchSummary> GetSearchSummaryAsync()
    {
        var totalCount = await _stockRepository.GetTotalCountAsync();
        var lastUpdated = await _stockRepository.GetLastUpdatedAsync();

        var marketCounts = new Dictionary<string, int>();
        foreach (var market in Enum.GetValues<Market>())
        {
            var stocks = await _stockRepository.GetByMarketAsync(market);
            marketCounts[market.GetDescription()] = stocks.Count;
        }

        return new StockSearchSummary
        {
            TotalCount = totalCount,
            LastUpdated = lastUpdated,
            MarketCounts = marketCounts
        };
    }

    private static DateTime? ParseListedDate(string? listedDateStr)
    {
        if (string.IsNullOrWhiteSpace(listedDateStr) || listedDateStr.Length != 8)
            return null;

        if (DateTime.TryParseExact(listedDateStr, "yyyyMMdd", null, 
                System.Globalization.DateTimeStyles.None, out var date))
        {
            return date;
        }

        return null;
    }
    
    private static Market NormalizeMarketName(string? securityGroup)
    {
        if (string.IsNullOrEmpty(securityGroup))
            return Market.Kospi;

        return securityGroup switch
        {
            var s when s.Contains("코스피") || s.Contains("KOSPI") => Market.Kospi,
            var s when s.Contains("코스닥") || s.Contains("KOSDAQ") => Market.Kosdaq,
            var s when s.Contains("코넥스") || s.Contains("KONEX") => Market.Konex,
            _ => Market.Kospi
        };
    }
}