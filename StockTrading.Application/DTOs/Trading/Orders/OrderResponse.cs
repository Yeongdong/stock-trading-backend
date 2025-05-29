using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.Trading.Orders;

public class OrderResponse
{
    [JsonPropertyName("rt_cd")]
    public string ReturnCode { get; set; } // 성공 실패 여부
    
    [JsonPropertyName("msg_cd")]
    public string MessageCode { get; set; } // 응답코드
    
    [JsonPropertyName("msg1")]
    public string Message { get; set; } // 응답메시지
    
    [JsonPropertyName("output")]
    public OrderResponseOutput? Output { get; set; } // 응답상세
}