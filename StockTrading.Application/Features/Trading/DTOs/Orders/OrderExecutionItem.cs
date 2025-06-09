namespace StockTrading.Application.Features.Trading.DTOs.Orders;

public class OrderExecutionItem
{
    public string OrderDate { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string StockCode { get; init; } = string.Empty;
    public string StockName { get; init; } = string.Empty;
    public string OrderSide { get; init; } = string.Empty;
    public int OrderQuantity { get; init; }
    public decimal OrderPrice { get; init; }
    public int ExecutedQuantity { get; init; }
    public decimal ExecutedPrice { get; init; }
    public decimal ExecutedAmount { get; init; }
    public string OrderStatus { get; init; } = string.Empty;
    public string ExecutionTime { get; init; } = string.Empty;
}