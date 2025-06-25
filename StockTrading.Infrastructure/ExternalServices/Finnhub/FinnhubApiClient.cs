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
       var queryString = $"q={Uri.EscapeDataString(request.Query)}";
       if (!string.IsNullOrEmpty(request.Exchange))
           queryString += $"&exchange={Uri.EscapeDataString(request.Exchange)}";

       var fullUrl = $"{_settings.BaseUrl.TrimEnd('/')}/{_settings.Endpoints.SymbolSearchPath.TrimStart('/')}?{queryString}";

       var httpRequest = new HttpRequestMessage(HttpMethod.Get, fullUrl);
       SetStandardHeaders(httpRequest);

       var response = await _httpClient.SendAsync(httpRequest);
       var responseContent = await ValidateAndReadResponse(response);

       if (responseContent.StartsWith("<") || responseContent.Contains("html"))
           throw new Exception("Finnhub API 인증 실패 또는 잘못된 요청입니다.");

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
           .Select(CreateForeignStockInfo)
           .ToList();

       return new ForeignStockSearchResult { Stocks = stocks, Count = stocks.Count };
   }

   private static ForeignStockInfo CreateForeignStockInfo(FinnhubSymbolInfo symbol) => new()
   {
       Symbol = symbol.Symbol,
       DisplaySymbol = symbol.DisplaySymbol,
       Description = symbol.Description,
       Type = symbol.Type,
       Currency = GetCurrency(symbol.Symbol),
       Exchange = GetExchange(symbol.Symbol),
       Country = GetCountry(symbol.Symbol)
   };

   private static readonly Dictionary<string, (string Currency, string Exchange, string Country)> SuffixMappings = new()
   {
       { ".TO", ("CAD", "TSE", "CA") },
       { ".L", ("GBP", "LSE", "GB") },
       { ".T", ("JPY", "TSE", "JP") },
       { ".HK", ("HKD", "HKEX", "HK") },
       { ".SW", ("CHF", "SIX", "CH") },
       { ".PA", ("EUR", "EPA", "FR") },
       { ".DE", ("EUR", "XETRA", "DE") }
   };

   private static string GetCurrency(string symbol) => 
       SuffixMappings.FirstOrDefault(kvp => symbol.EndsWith(kvp.Key)).Value.Currency ?? "USD";

   private static string GetExchange(string symbol) => 
       SuffixMappings.FirstOrDefault(kvp => symbol.EndsWith(kvp.Key)).Value.Exchange ?? "NASDAQ";

   private static string GetCountry(string symbol) => 
       SuffixMappings.FirstOrDefault(kvp => symbol.EndsWith(kvp.Key)).Value.Country ?? "US";
}