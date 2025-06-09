using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

public class KisPeriodPriceOutput1
{
    [JsonPropertyName("prdy_vrss")] public string PriceChange { get; init; } = string.Empty;
    [JsonPropertyName("prdy_vrss_sign")] public string ChangeSign { get; init; } = string.Empty;
    [JsonPropertyName("prdy_ctrt")] public string ChangeRate { get; init; } = string.Empty;
    [JsonPropertyName("stck_prdy_clpr")] public string PreviousClose { get; init; } = string.Empty;
    [JsonPropertyName("acml_vol")] public string AccumulatedVolume { get; init; } = string.Empty;
    [JsonPropertyName("acml_tr_pbmn")] public string AccumulatedAmount { get; init; } = string.Empty;
    [JsonPropertyName("hts_kor_isnm")] public string StockName { get; init; } = string.Empty;
    [JsonPropertyName("stck_prpr")] public string CurrentPrice { get; init; } = string.Empty;
    [JsonPropertyName("stck_shrn_iscd")] public string StockCode { get; init; } = string.Empty;
    [JsonPropertyName("stck_mxpr")] public string UpperLimit { get; init; } = string.Empty;
    [JsonPropertyName("stck_llam")] public string LowerLimit { get; init; } = string.Empty;
    [JsonPropertyName("stck_oprc")] public string OpenPrice { get; init; } = string.Empty;
    [JsonPropertyName("stck_hgpr")] public string HighPrice { get; init; } = string.Empty;
    [JsonPropertyName("stck_lwpr")] public string LowPrice { get; init; } = string.Empty;
    [JsonPropertyName("pvol")] public string PreviousVolume { get; init; } = string.Empty;
    [JsonPropertyName("stck_prdy_oprc")] public string PreviousOpenPrice { get; init; } = string.Empty;
    [JsonPropertyName("stck_prdy_hgpr")] public string PreviousHighPrice { get; init; } = string.Empty;
    [JsonPropertyName("stck_prdy_lwpr")] public string PreviousLowPrice { get; init; } = string.Empty;
    [JsonPropertyName("askp")] public string AskPrice { get; init; } = string.Empty;
    [JsonPropertyName("bidp")] public string BidPrice { get; init; } = string.Empty;
    [JsonPropertyName("prdy_vrss_vol")] public string PreviousVolumeChange { get; init; } = string.Empty;
    [JsonPropertyName("vol_tnrt")] public string VolumeTurnoverRate { get; init; } = string.Empty;
    [JsonPropertyName("stck_fcam")] public string FaceAmount { get; init; } = string.Empty;
    [JsonPropertyName("lstn_stcn")] public string ListedStockCount { get; init; } = string.Empty;
    [JsonPropertyName("cpfn")] public string CapitalFund { get; init; } = string.Empty;
    [JsonPropertyName("hts_avls")] public string MarketCapitalization { get; init; } = string.Empty;
    [JsonPropertyName("per")] public string PER { get; init; } = string.Empty;
    [JsonPropertyName("eps")] public string EPS { get; init; } = string.Empty;
    [JsonPropertyName("pbr")] public string PBR { get; init; } = string.Empty;
}