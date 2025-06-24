using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 해외 주식 현재가 조회 응답 데이터
/// </summary>
public class KisOverseasPriceData
{
    /// <summary>종목코드</summary>
    [JsonPropertyName("symb")]
    public string StockCode { get; init; } = string.Empty;

    /// <summary>종목명</summary>
    [JsonPropertyName("issu_name")]
    public string StockName { get; init; } = string.Empty;

    /// <summary>현재가</summary>
    [JsonPropertyName("last")]
    public string CurrentPrice { get; init; } = string.Empty;

    /// <summary>전일대비</summary>
    [JsonPropertyName("diff")]
    public string PriceChange { get; init; } = string.Empty;

    /// <summary>등락률</summary>
    [JsonPropertyName("rate")]
    public string ChangeRate { get; init; } = string.Empty;

    /// <summary>시가</summary>
    [JsonPropertyName("open")]
    public string OpenPrice { get; init; } = string.Empty;

    /// <summary>고가</summary>
    [JsonPropertyName("high")]
    public string HighPrice { get; init; } = string.Empty;

    /// <summary>저가</summary>
    [JsonPropertyName("low")]
    public string LowPrice { get; init; } = string.Empty;

    /// <summary>거래량</summary>
    [JsonPropertyName("tvol")]
    public string Volume { get; init; } = string.Empty;

    /// <summary>통화</summary>
    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;

    /// <summary>시장 상태</summary>
    [JsonPropertyName("mkt_st")]
    public string MarketStatus { get; init; } = string.Empty;
}