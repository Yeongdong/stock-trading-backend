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
            .Where(item => !string.IsNullOrEmpty(item.Code) && 
                          item.Code.Length == 6 && 
                          !string.IsNullOrEmpty(item.Name))
            .Select(item => new Stock(
                code: item.Code,
                name: item.Name,
                sector: item.Sector ?? "기타",
                market: NormalizeMarketName(item.Market),
                englishName: item.EnglishName))
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

    private static Market NormalizeMarketName(string market)
    {
        return market switch
        {
            "코스피" or "KOSPI" or "STK" => Market.Kospi,
            "코스닥" or "KOSDAQ" or "KSQ" => Market.Kosdaq,
            "코넥스" or "KONEX" or "KNX" => Market.Konex,
            _ => Market.Kospi
        };
    }
}