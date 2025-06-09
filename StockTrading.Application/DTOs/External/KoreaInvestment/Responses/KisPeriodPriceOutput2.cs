using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

public class KisPeriodPriceOutput2
{
    [JsonPropertyName("stck_bsop_date")] public string BusinessDate { get; init; } = string.Empty; // 주식영업일자
    [JsonPropertyName("stck_clpr")] public string ClosePrice { get; init; } = string.Empty; // 종가
    [JsonPropertyName("stck_oprc")] public string OpenPrice { get; init; } = string.Empty; // 시가
    [JsonPropertyName("stck_hgpr")] public string HighPrice { get; init; } = string.Empty; // 고가
    [JsonPropertyName("stck_lwpr")] public string LowPrice { get; init; } = string.Empty; // 저가
    [JsonPropertyName("acml_vol")] public string Volume { get; init; } = string.Empty; // 누적거래량
    [JsonPropertyName("acml_tr_pbmn")] public string TradingAmount { get; init; } = string.Empty; // 누적거래대금
    [JsonPropertyName("flng_cls_code")] public string FlagCode { get; init; } = string.Empty; // 등락구분코드
    [JsonPropertyName("prtt_rate")] public string SplitRate { get; init; } = string.Empty; // 분할비율
    [JsonPropertyName("mod_yn")] public string ModifiedFlag { get; init; } = string.Empty; // 수정주가여부
    [JsonPropertyName("prdy_vrss_sign")] public string ChangeSign { get; init; } = string.Empty; // 전일대비부호
    [JsonPropertyName("prdy_vrss")] public string PriceChange { get; init; } = string.Empty; // 전일대비
    [JsonPropertyName("revl_issu_reas")] public string RevaluationReason { get; init; } = string.Empty; // 재평가사유코드
}