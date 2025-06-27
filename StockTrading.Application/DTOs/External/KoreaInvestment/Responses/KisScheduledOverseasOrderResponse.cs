using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 해외주식 예약주문 응답
/// </summary>
public class KisScheduledOverseasOrderResponse : KisBaseResponse<KisScheduledOverseasOrderData>
{
}

/// <summary>
/// KIS API 해외주식 예약주문 응답 데이터
/// </summary>
public class KisScheduledOverseasOrderData
{
    /// <summary>
    /// 한국거래소전송주문조직번호 (미국주문용)
    /// </summary>
    [JsonPropertyName("ODNO")]
    public string? OrderNumber { get; set; }

    /// <summary>
    /// 예약주문접수일자 (아시아주문용)
    /// </summary>
    [JsonPropertyName("RSVN_ORD_RCIT_DT")]
    public string? ReservedOrderDate { get; set; }

    /// <summary>
    /// 해외예약주문번호 (아시아주문용)
    /// </summary>
    [JsonPropertyName("OVRS_RSVN_ODNO")]
    public string? OverseasReservedOrderNumber { get; set; }
}