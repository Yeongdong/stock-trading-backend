using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.Finnhub.Responses;

public class FinnhubSymbolSearchResponse
{
    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("result")]
    public List<FinnhubSymbolInfo> Result { get; init; } = [];
}

public class FinnhubSymbolInfo
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("displaySymbol")]
    public string DisplaySymbol { get; set; } = string.Empty;

    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}