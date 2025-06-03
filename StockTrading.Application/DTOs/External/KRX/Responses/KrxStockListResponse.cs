using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KRX.Responses;

/// <summary>
/// KRX OpenAPI 상장법인목록 응답 모델
/// </summary>
public class KrxStockListResponse
{
    [JsonPropertyName("OutBlock_1")] public List<KrxStockItem> Stocks { get; init; } = [];
}