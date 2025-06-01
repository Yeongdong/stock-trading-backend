using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 주문 응답 데이터
/// </summary>
public class KisOrderData
{
    /// <summary>
    /// 거래소코드
    /// </summary>
    [JsonPropertyName("KRX_FWDG_ORD_ORGNO")]
    public string KrxForwardOrderOrgNo { get; init; } = string.Empty;

    /// <summary>
    /// 주문번호
    /// </summary>
    [JsonPropertyName("ODNO")]
    public string OrderNumber { get; init; } = string.Empty;

    /// <summary>
    /// 주문시간
    /// </summary>
    [JsonPropertyName("ORD_TMD")]
    public string OrderTime { get; init; } = string.Empty;
}