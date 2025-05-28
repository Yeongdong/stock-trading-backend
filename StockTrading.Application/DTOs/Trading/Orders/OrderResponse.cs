namespace StockTrading.Application.DTOs.Trading.Orders;

public class OrderResponse
{
    public string rt_cd { get; init; } // 성공 실패 여부
    public string msg_cd { get; init; } // 응답코드
    public string msg1 { get; init; } // 응답메시지
    public List<OrderResponseOutput> output { get; init; } = []; // 응답상세
}