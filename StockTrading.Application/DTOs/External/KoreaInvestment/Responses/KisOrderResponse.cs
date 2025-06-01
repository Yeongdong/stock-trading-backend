using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

public class KisOrderResponse: KisBaseResponse<KisOrderData>
{
    [JsonPropertyName("rt_cd")]
    public string ReturnCode { get; init; } // 성공 실패 여부
    
    [JsonPropertyName("msg_cd")]
    public string MessageCode { get; init; } // 응답코드
    
    [JsonPropertyName("msg1")]
    public string Message { get; init; } // 응답메시지
    
    [JsonPropertyName("output")]
    public KisOrderData Output { get; init; } // 응답상세
}