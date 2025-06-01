using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 매수가능조회 응답 데이터
/// </summary>
public class KisBuyableInquiryData
{
    /// <summary>
    /// 종목코드
    /// </summary>
    [JsonPropertyName("pdno")]
    public string StockCode { get; init; } = string.Empty;

    /// <summary>
    /// 종목명
    /// </summary>
    [JsonPropertyName("prdt_name")]
    public string StockName { get; init; } = string.Empty;

    /// <summary>
    /// 매수가능금액
    /// </summary>
    [JsonPropertyName("ord_psbl_cash")]
    public string BuyableAmount { get; init; } = string.Empty;

    /// <summary>
    /// 매수가능수량
    /// </summary>
    [JsonPropertyName("ord_psbl_qty")]
    public string BuyableQuantity { get; init; } = string.Empty;

    /// <summary>
    /// 주문가능금액
    /// </summary>
    [JsonPropertyName("psbl_amt")]
    public string OrderableAmount { get; init; } = string.Empty;

    /// <summary>
    /// 현재가
    /// </summary>
    [JsonPropertyName("stck_prpr")]
    public string CurrentPrice { get; init; } = string.Empty;

    /// <summary>
    /// 단위수량
    /// </summary>
    [JsonPropertyName("unit_qty")]
    public string UnitQuantity { get; init; } = "1";
}