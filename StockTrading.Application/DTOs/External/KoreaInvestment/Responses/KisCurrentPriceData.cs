using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 주식 현재가 조회 응답 데이터
/// </summary>
public class KisCurrentPriceData
{
    /// <summary>종목코드</summary>
    [JsonPropertyName("mksc_shrn_iscd")]
    public string StockCode { get; init; } = string.Empty;

    /// <summary>종목명</summary>
    [JsonPropertyName("hts_kor_isnm")]
    public string StockName { get; init; } = string.Empty;

    /// <summary>현재가</summary>
    [JsonPropertyName("stck_prpr")]
    public string CurrentPrice { get; init; } = string.Empty;

    /// <summary>전일대비</summary>
    [JsonPropertyName("prdy_vrss")]
    public string PriceChange { get; init; } = string.Empty;

    /// <summary>전일대비율</summary>
    [JsonPropertyName("prdy_vrss_sign")]
    public string ChangeSign { get; init; } = string.Empty;

    /// <summary>등락률</summary>
    [JsonPropertyName("prdy_ctrt")]
    public string ChangeRate { get; init; } = string.Empty;

    /// <summary>시가</summary>
    [JsonPropertyName("stck_oprc")]
    public string OpenPrice { get; init; } = string.Empty;

    /// <summary>고가</summary>
    [JsonPropertyName("stck_hgpr")]
    public string HighPrice { get; init; } = string.Empty;

    /// <summary>저가</summary>
    [JsonPropertyName("stck_lwpr")]
    public string LowPrice { get; init; } = string.Empty;

    /// <summary>누적거래량</summary>
    [JsonPropertyName("acml_vol")]
    public string Volume { get; init; } = string.Empty;
}