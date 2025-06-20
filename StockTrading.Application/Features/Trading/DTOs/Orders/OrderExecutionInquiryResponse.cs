using System.Text.Json.Serialization;

namespace StockTrading.Application.Features.Trading.DTOs.Orders;

public class OrderExecutionInquiryResponse
{
    [JsonPropertyName("executions")] public List<OrderExecutionItem> Executions { get; init; } = [];
    [JsonPropertyName("totalCount")] public int TotalCount { get; init; }
    [JsonPropertyName("hasMore")] public bool HasMore { get; init; }
}