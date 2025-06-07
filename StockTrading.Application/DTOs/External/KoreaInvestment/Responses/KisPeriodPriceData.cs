using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

public class KisPeriodPriceData
{
    [JsonPropertyName("stck_bsop_date")] public string BusinessDate { get; init; } = string.Empty;
    [JsonPropertyName("stck_clpr")] public string ClosePrice { get; init; } = string.Empty;
    [JsonPropertyName("stck_oprc")] public string OpenPrice { get; init; } = string.Empty;
    [JsonPropertyName("stck_hgpr")] public string HighPrice { get; init; } = string.Empty;
    [JsonPropertyName("stck_lwpr")] public string LowPrice { get; init; } = string.Empty;
    [JsonPropertyName("acml_vol")] public string Volume { get; init; } = string.Empty;
    [JsonPropertyName("acml_tr_pbmn")] public string TradingValue { get; init; } = string.Empty;
    [JsonPropertyName("prdy_vrss_sign")] public string ChangeSign { get; init; } = string.Empty;
    [JsonPropertyName("prdy_vrss")] public string PriceChange { get; init; } = string.Empty;
    [JsonPropertyName("flng_cls_code")] public string FlagCode { get; init; } = string.Empty;
    [JsonPropertyName("prtt_rate")] public string SplitRate { get; init; } = string.Empty;
    [JsonPropertyName("mod_yn")] public string ModifiedYn { get; init; } = string.Empty;
    [JsonPropertyName("revl_issu_reas")] public string RevaluationReason { get; init; } = string.Empty;
}