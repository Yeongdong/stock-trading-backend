using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

public class KisPeriodPriceOutput1
{
    [JsonPropertyName("hts_kor_isnm")] public string StockName { get; init; } = string.Empty;
    [JsonPropertyName("stck_prpr")] public string CurrentPrice { get; init; } = string.Empty;
    [JsonPropertyName("prdy_vrss")] public string PriceChange { get; init; } = string.Empty;
    [JsonPropertyName("prdy_vrss_sign")] public string ChangeSign { get; init; } = string.Empty;
    [JsonPropertyName("prdy_ctrt")] public string ChangeRate { get; init; } = string.Empty;
    [JsonPropertyName("acml_vol")] public string Volume { get; init; } = string.Empty;
    [JsonPropertyName("acml_tr_pbmn")] public string TradingValue { get; init; } = string.Empty;
}