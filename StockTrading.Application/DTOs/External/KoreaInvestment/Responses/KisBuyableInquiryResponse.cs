using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 매수가능조회 응답
/// </summary>
public class KisBuyableInquiryResponse: KisBaseResponse<KisBuyableInquiryData>
{
    [JsonPropertyName("rt_cd")]
    public string ReturnCode { get; init; } = string.Empty;

    [JsonPropertyName("msg_cd")]
    public string MessageCode { get; init; } = string.Empty;

    [JsonPropertyName("msg1")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("output")]
    public KisBuyableInquiryData? Output { get; init; }
}
