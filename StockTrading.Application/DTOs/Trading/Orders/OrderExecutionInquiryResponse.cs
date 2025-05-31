namespace StockTrading.Application.DTOs.Trading.Orders;

/// <summary>
/// 주문체결조회 응답 DTO
/// </summary>
public class OrderExecutionInquiryResponse
{
    public List<OrderExecutionItem> ExecutionItems { get; init; } = new();
    public int TotalCount { get; init; }
    public bool HasMore { get; init; }
}