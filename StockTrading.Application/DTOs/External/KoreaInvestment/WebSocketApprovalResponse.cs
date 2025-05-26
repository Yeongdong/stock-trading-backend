using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment;

public class WebSocketApprovalResponse
{
    [JsonPropertyName("approval_key")]
    public string ApprovalKey { get; set; }
}