namespace StockTrading.Application.Features.Trading.DTOs.Orders;

public class OrderExecutionInquiryResponse
{
    public List<OrderExecutionItem> ExecutionItems { get; init; } = new();
    public int TotalCount { get; init; }
    public bool HasMore { get; init; }
}