using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.Finnhub.Responses;

public class FinnhubSymbolSearchResponse
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("result")]
    public List<FinnhubSymbolInfo> Result { get; set; } = [];
}

public class FinnhubSymbolInfo
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("displaySymbol")]
    public string DisplaySymbol { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("mic")]
    public string Mic { get; set; } = string.Empty;
}