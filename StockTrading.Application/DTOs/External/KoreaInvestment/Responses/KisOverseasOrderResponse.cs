using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 해외 주식 주문 응답
/// </summary>
public class KisOverseasOrderResponse : KisBaseResponse<KisOverseasOrderData>
{
}

/// <summary>
/// KIS API 해외 주식 주문 응답 데이터
/// </summary>
public class KisOverseasOrderData
{
    /// <summary>
    /// 주문번호
    /// </summary>
    [JsonPropertyName("odno")]
    public string OrderNumber { get; set; } = null!;

    /// <summary>
    /// 주문시각
    /// </summary>
    [JsonPropertyName("ord_tmd")]
    public string OrderTime { get; set; } = null!;
}