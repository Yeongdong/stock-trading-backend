using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

public class KisWebSocketApprovalResponse
{
    [JsonPropertyName("approval_key")]
    public string ApprovalKey { get; init; }
}