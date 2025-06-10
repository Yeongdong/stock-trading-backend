using System.Text.Json.Serialization;

namespace StockTrading.Application.Features.Trading.DTOs.Orders;

public class OrderExecutionItem
{
    [JsonPropertyName("orderDate")]
    public string OrderDate { get; init; } = string.Empty;
    
    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; init; } = string.Empty;
    
    [JsonPropertyName("stockCode")]
    public string StockCode { get; init; } = string.Empty;
    
    [JsonPropertyName("stockName")]
    public string StockName { get; init; } = string.Empty;
    
    [JsonPropertyName("orderSide")]
    public string OrderSide { get; init; } = string.Empty;
    
    [JsonPropertyName("orderQuantity")]
    public int OrderQuantity { get; init; }
    
    [JsonPropertyName("orderPrice")]
    public decimal OrderPrice { get; init; }
    
    [JsonPropertyName("executedQuantity")]
    public int ExecutedQuantity { get; init; }
    
    [JsonPropertyName("executedPrice")]
    public decimal ExecutedPrice { get; init; }
    
    [JsonPropertyName("executedAmount")]
    public decimal ExecutedAmount { get; init; }
    
    [JsonPropertyName("orderStatus")]
    public string OrderStatus { get; init; } = string.Empty;
    
    [JsonPropertyName("executionTime")]
    public string ExecutionTime { get; init; } = string.Empty;
}