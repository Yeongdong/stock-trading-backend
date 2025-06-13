using System.Text.Json.Serialization;

namespace StockTrading.Application.Features.Trading.DTOs.Inquiry;

public class PeriodPriceResponse
{
    [JsonPropertyName("stockCode")] public string StockCode { get; set; } = string.Empty;
    [JsonPropertyName("stockName")] public string StockName { get; set; } = string.Empty;
    [JsonPropertyName("currentPrice")] public decimal CurrentPrice { get; set; }
    [JsonPropertyName("priceChange")] public decimal PriceChange { get; set; }
    [JsonPropertyName("changeRate")] public decimal ChangeRate { get; set; }
    [JsonPropertyName("changeSign")] public string ChangeSign { get; set; } = string.Empty;
    [JsonPropertyName("totalVolume")] public long TotalVolume { get; set; }
    [JsonPropertyName("totalTradingValue")] public long TotalTradingValue { get; set; }
    [JsonPropertyName("priceData")] public List<PeriodPriceData> PriceData { get; init; } = [];
}

public class PeriodPriceData
{
    [JsonPropertyName("date")] public string Date { get; set; } = string.Empty;
    [JsonPropertyName("openPrice")] public decimal OpenPrice { get; set; }
    [JsonPropertyName("highPrice")] public decimal HighPrice { get; set; }
    [JsonPropertyName("lowPrice")] public decimal LowPrice { get; set; }
    [JsonPropertyName("closePrice")] public decimal ClosePrice { get; set; }
    [JsonPropertyName("volume")] public long Volume { get; set; }
    [JsonPropertyName("tradingValue")] public long TradingValue { get; set; }
    [JsonPropertyName("priceChange")] public decimal PriceChange { get; set; }
    [JsonPropertyName("changeSign")] public string ChangeSign { get; set; } = string.Empty;
    [JsonPropertyName("flagCode")] public string FlagCode { get; set; } = string.Empty;
    [JsonPropertyName("splitRate")] public decimal SplitRate { get; set; }
}