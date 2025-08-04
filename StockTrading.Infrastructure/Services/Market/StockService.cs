using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.External.KoreaInvestment.Requests;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Market.DTOs.Stock;
using StockTrading.Application.Features.Market.Repositories;
using StockTrading.Application.Features.Market.Services;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Entities;
using StockTrading.Domain.Enums;
using StockTrading.Domain.Extensions;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common.Helpers;
using StockTrading.Infrastructure.ExternalServices.KRX;

namespace StockTrading.Infrastructure.Services.Market;

public class StockService : IStockService
{
    private readonly IStockRepository _stockRepository;
    private readonly IForeignStockRepository _foreignStockRepository;
    private readonly IStockCacheService _stockCacheService;
    private readonly KrxApiClient _krxApiClient;
    private readonly IKisBalanceApiClient _kisBalanceApiClient;
    private readonly ILogger<StockService> _logger;

    public StockService(IStockRepository stockRepository, IForeignStockRepository foreignStockRepository,
        IStockCacheService stockCacheService,
        KrxApiClient krxApiClient, IKisBalanceApiClient kisBalanceApiClient, ILogger<StockService> logger)
    {
        _stockRepository = stockRepository;
        _foreignStockRepository = foreignStockRepository;
        _stockCacheService = stockCacheService;
        _krxApiClient = krxApiClient;
        _kisBalanceApiClient = kisBalanceApiClient;
        _logger = logger;
    }

    #region 국내 주식 검색 및 조회

    public async Task<StockSearchResponse> SearchStocksAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new StockSearchResponse
            {
                Results = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
                HasMore = false
            };
        }

        return await _stockCacheService.SearchStocksAsync(searchTerm, page, pageSize);
    }

    public async Task<StockSearchResult?> GetStockByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            return null;

        // 1. 메모리 캐시에서 조회
        var cachedResult = await _stockCacheService.GetStockByCodeAsync(code);
        if (cachedResult != null)
            return cachedResult;

        // 2. DB에서 조회
        var stock = await _stockRepository.GetByCodeAsync(code);
        return stock != null ? MapToSearchResult(stock) : null;
    }

    public async Task<List<StockSearchResult>> GetStocksByMarketAsync(Domain.Enums.Market market)
    {
        var stocks = await _stockRepository.GetByMarketAsync(market);
        return stocks.Select(MapToSearchResult).ToList();
    }

    public async Task<StockSearchSummary> GetSearchSummaryAsync()
    {
        // 메모리 캐시에서 통계 계산
        var cacheStats = _stockCacheService.GetCacheStats();

        if (cacheStats.TotalStocks > 0)
        {
            // 메모리 캐시에서 시장별 카운트 계산
            var marketCounts = await GetMarketCountsFromCacheAsync();

            return new StockSearchSummary
            {
                TotalCount = cacheStats.TotalStocks,
                LastUpdated = cacheStats.LastLoadedAt,
                MarketCounts = marketCounts
            };
        }

        // 캐시가 없으면 DB에서 조회
        var totalCount = await _stockRepository.GetTotalCountAsync();
        var dbMarketCounts = await GetMarketCountsFromDbAsync();

        return new StockSearchSummary
        {
            TotalCount = totalCount,
            LastUpdated = DateTime.UtcNow,
            MarketCounts = dbMarketCounts
        };
    }

    #endregion

    #region 데이터 동기화

    public async Task SyncDomesticStockDataAsync()
    {
        _logger.LogInformation("국내 주식 데이터 동기화 시작");

        // 1. KRX API에서 최신 주식 데이터 가져오기
        var stockListResponse = await _krxApiClient.GetStockListAsync();
        var validStocks = stockListResponse.Stocks
            .Where(item => !string.IsNullOrWhiteSpace(item.Code) && item.Code.Length == 6)
            .Select(item => new Stock(
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

        // 2. DB 업데이트
        await _stockRepository.BulkUpsertAsync(validStocks);

        // 3. 메모리 캐시 갱신
        await _stockCacheService.RefreshCacheAsync();

        _logger.LogInformation("국내 주식 데이터 동기화 완료: {Count}개 종목", validStocks.Count);
    }

    #endregion

    #region 해외 주식

    public async Task<ForeignStockSearchResult> SearchForeignStocksAsync(ForeignStockSearchRequest request,
        UserInfo userInfo)
    {
        KisValidationHelper.ValidateUserForKisApi(userInfo);

        var searchTerm = request.Query?.Trim();
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new ForeignStockSearchResult { Market = request.Market };

        _logger.LogInformation("해외주식 검색 시작: 사용자={UserId}, 검색어={SearchTerm}, 시장={Market}", userInfo.Id, searchTerm,
            request.Market);

        // 1. DB에서 검색
        var dbResults = await _foreignStockRepository.SearchByTermAsync(searchTerm, request.Limit);
        if (dbResults.Count != 0)
        {
            var dbResult = ConvertToForeignStockSearchResult(dbResults, request.Market);
            _logger.LogInformation("DB에서 해외주식 검색 완료: {Count}개 종목", dbResult.Count);
            return dbResult;
        }

        // 2. KIS API 호출
        var exchangeCode = MapMarketToExchangeCode(request.Market);
        var kisRequest = BuildKisOverseasSearchRequest(searchTerm, exchangeCode);

        var kisResponse = await _kisBalanceApiClient.SearchOverseasStocksAsync(kisRequest, userInfo);
        var apiResult = ConvertKisResponseToForeignStockResult(kisResponse, request.Market);

        // 3. 새로운 종목들을 DB에 저장
        await SaveNewForeignStocksAsync(apiResult.Stocks);

        _logger.LogInformation("KIS API 해외주식 검색 완료: {Count}개 종목", apiResult.Count);

        return apiResult;
    }

    private static KisOverseasStockSearchRequest BuildKisOverseasSearchRequest(string searchTerm, string exchangeCode)
    {
        return new KisOverseasStockSearchRequest
        {
            AUTH = "",
            EXCD = exchangeCode,
            KEYB = "",
        };
    }

    private static string MapMarketToExchangeCode(string market)
    {
        return market.ToLower() switch
        {
            "nasdaq" or "nas" => "NAS",
            "nyse" or "nys" => "NYS",
            "amex" or "ams" => "AMS",
            "tokyo" or "tse" => "TSE",
            "hongkong" or "hks" => "HKS",
            "shanghai" or "shs" => "SHS",
            "shenzhen" or "szs" => "SZS",
            "hanoi" or "hnx" => "HNX",
            "hochiminh" or "hsx" => "HSX",
            _ => throw new ArgumentException($"지원하지 않는 시장입니다: {market}")
        };
    }

    #endregion

    #region Private Helper Methods

    private async Task<Dictionary<string, int>> GetMarketCountsFromCacheAsync()
    {
        // 메모리 캐시에서 모든 종목 조회 후 그룹핑
        var allStocks = await _stockCacheService.SearchStocksAsync("", 1, int.MaxValue);

        return allStocks.Results
            .GroupBy(s => s.Market)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task<Dictionary<string, int>> GetMarketCountsFromDbAsync()
    {
        // DB에서 모든 종목 조회 후 그룹핑 (캐시 없을 때만)
        var allStocks = await _stockRepository.GetAllAsync();

        return allStocks
            .GroupBy(s => s.Market.ToString())
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static StockSearchResult MapToSearchResult(Stock stock)
    {
        return new StockSearchResult
        {
            Code = stock.Code,
            Name = stock.Name,
            Sector = stock.Sector,
            Market = stock.Market.ToString()
        };
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

    private static ForeignStockSearchResult ConvertKisResponseToForeignStockResult(
        KisOverseasStockSearchResponse kisResponse, string requestedMarket)
    {
        var result = new ForeignStockSearchResult
        {
            Market = requestedMarket,
            Status = kisResponse.output1?.stat ?? "",
            Count = 0,
            Stocks = []
        };

        if (kisResponse.output2 == null || kisResponse.output2.Count == 0)
            return result;

        var requestedExchangeCode = MapMarketToExchangeCode(requestedMarket);

        var filteredStocks = kisResponse.output2
            .Where(item => item.excd == requestedExchangeCode)
            .Where(item => !string.IsNullOrWhiteSpace(item.symb) && !string.IsNullOrWhiteSpace(item.name))
            .ToList();

        if (filteredStocks.Count == 0)
            return result;

        var (currency, country) = GetMarketInfo(requestedExchangeCode);

        result.Count = filteredStocks.Count;
        result.Stocks = filteredStocks
            .Select(item => ConvertKisItemToForeignStockInfo(item, currency, country))
            .ToList();

        return result;
    }

    private static ForeignStockInfo ConvertKisItemToForeignStockInfo(KisOverseasStockItem item, string currency,
        string country)
    {
        return new ForeignStockInfo
        {
            Symbol = item.symb,
            DisplaySymbol = item.rsym,
            Description = item.name,
            EnglishName = item.ename,
            Type = "Common Stock",
            Exchange = item.excd,
            Currency = currency,
            Country = country,
            CurrentPrice = decimal.TryParse(item.last, out var price) ? price : 0,
            ChangeRate = decimal.TryParse(item.rate, out var rate) ? rate : 0,
            ChangeAmount = decimal.TryParse(item.diff, out var diff) ? diff : 0,
            ChangeSign = item.sign,
            Volume = long.TryParse(item.tvol, out var volume) ? volume : 0,
            MarketCap = long.TryParse(item.valx, out var marketCap) ? marketCap : 0,
            PER = decimal.TryParse(item.per, out var per) ? per : null,
            EPS = decimal.TryParse(item.eps, out var eps) ? eps : null,
            IsTradable = item.e_ordyn == "○" || item.e_ordyn == "O",
            Rank = int.TryParse(item.rank, out var rank) ? rank : 0
        };
    }

    private static (string Currency, string Country) GetMarketInfo(string exchangeCode)
    {
        return exchangeCode switch
        {
            "NAS" or "NYS" or "AMS" => ("USD", "US"),
            "TSE" => ("JPY", "JP"),
            "HKS" => ("HKD", "HK"),
            "SHS" or "SZS" => ("CNY", "CN"),
            "HNX" or "HSX" => ("VND", "VN"),
            _ => ("USD", "US")
        };
    }

    private async Task SaveNewForeignStocksAsync(IList<ForeignStockInfo> stockInfos)
    {
        if (!stockInfos.Any()) return;

        var symbols = stockInfos.Select(s => s.Symbol).ToList();
        var existingStocks = await _foreignStockRepository.GetBySymbolsAsync(symbols);
        var existingSymbols = existingStocks.Select(s => s.Symbol).ToHashSet();

        var newStocks = stockInfos
            .Where(info => !existingSymbols.Contains(info.Symbol))
            .Select(info => new ForeignStock(
                info.Symbol,
                info.DisplaySymbol,
                info.Description,
                info.Type,
                info.Currency,
                info.Exchange,
                info.Country,
                "" // Sector 정보는 KIS API에서 제공하지 않음
            ))
            .ToList();

        if (newStocks.Count != 0)
        {
            await _foreignStockRepository.AddRangeAsync(newStocks);
            _logger.LogInformation("새로운 해외주식 {Count}개 DB 저장 완료", newStocks.Count);
        }
    }

    private static ForeignStockSearchResult ConvertToForeignStockSearchResult(List<ForeignStock> dbStocks,
        string market)
    {
        return new ForeignStockSearchResult
        {
            Market = market,
            Status = "DB 조회",
            Count = dbStocks.Count,
            Stocks = dbStocks.Select(stock => new ForeignStockInfo
            {
                Symbol = stock.Symbol,
                DisplaySymbol = stock.DisplaySymbol,
                Description = stock.Description,
                EnglishName = stock.Description, // DB에는 영문명이 따로 없으므로
                Type = stock.Type,
                Currency = stock.Currency,
                Exchange = stock.Exchange,
                Country = stock.Country,
                // 가격 정보는 DB에 없으므로 0으로 설정 (실시간 조회 필요시 별도 API 호출)
                CurrentPrice = 0,
                ChangeRate = 0,
                ChangeAmount = 0,
                ChangeSign = "3", // 보합
                Volume = 0,
                MarketCap = 0,
                PER = null,
                EPS = null,
                IsTradable = true,
                Rank = 0
            }).ToList()
        };
    }

    #endregion
}