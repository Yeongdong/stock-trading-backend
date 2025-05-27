namespace StockTrading.Application.DTOs.Trading.Orders;

public class OrderResponse
{
    public string rt_cd { get; set; }               // 성공 실패 여부
    public string msg_cd { get; set; }              // 응답코드
    public string msg { get; set; }                 // 응답메시지
    public OrderInfo Info { get; set; }         // 응답상세
}