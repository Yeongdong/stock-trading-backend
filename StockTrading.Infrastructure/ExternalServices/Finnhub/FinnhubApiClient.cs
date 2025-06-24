using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.External.Finnhub.Responses;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Market.DTOs.Stock;
using StockTrading.Domain.Settings.ExternalServices;
using StockTrading.Infrastructure.ExternalServices.Finnhub.Common;

namespace StockTrading.Infrastructure.ExternalServices.Finnhub;

public class FinnhubApiClient : FinnhubApiClientBase, IFinnhubApiClient
{
    public FinnhubApiClient(HttpClient httpClient, IOptions<FinnhubSettings> settings,
        ILogger<FinnhubApiClient> logger) : base(httpClient, settings, logger)
    {
    }

    public async Task<ForeignStockSearchResult> SearchSymbolsAsync(ForeignStockSearchRequest request)
    {
        var queryParams = new Dictionary<string, string>
        {
            { "q", request.Query }
        };

        if (!string.IsNullOrEmpty(request.Exchange))
            queryParams.Add("exchange", request.Exchange);

        var url = BuildGetUrl(_settings.Endpoints.SymbolSearchPath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        SetStandardHeaders(httpRequest);

        var response = await _httpClient.SendAsync(httpRequest);
        var responseContent = await ValidateAndReadResponse(response);

        var finnhubResponse = JsonSerializer.Deserialize<FinnhubSymbolSearchResponse>(responseContent);

        return ConvertToForeignStockSearchResult(finnhubResponse, request.Limit);
    }

    private static ForeignStockSearchResult ConvertToForeignStockSearchResult(
        FinnhubSymbolSearchResponse? finnhubResponse, int limit)
    {
        if (finnhubResponse?.Result == null)
            return new ForeignStockSearchResult();

        var stocks = finnhubResponse.Result
            .Take(limit)
            .Select(symbol => new ForeignStockInfo
            {
                Symbol = symbol.Symbol,
                DisplaySymbol = symbol.DisplaySymbol,
                Description = symbol.Description,
                Type = symbol.Type,
                Currency = symbol.Currency,
                Exchange = ExtractExchangeFromMic(symbol.Mic),
                Country = ExtractCountryFromMic(symbol.Mic)
            })
            .ToList();

        return new ForeignStockSearchResult
        {
            Stocks = stocks,
            Count = stocks.Count
        };
    }

    private static string ExtractExchangeFromMic(string mic)
    {
        // MIC 코드에서 거래소명 추출
        return mic switch
        {
            "XNYS" => "NYSE",
            "XNAS" => "NASDAQ",
            "XLON" => "LSE",
            "XTSE" => "TSE",
            "XHKG" => "HKEX",
            _ => mic
        };
    }

    private static string ExtractCountryFromMic(string mic)
    {
        // MIC 코드에서 국가 추출
        return mic switch
        {
            "XNYS" or "XNAS" => "US",
            "XLON" => "GB",
            "XTSE" => "JP",
            "XHKG" => "HK",
            _ => "Unknown"
        };
    }
}