using System.Text.Json.Serialization;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;

public class WebSocketApprovalResponse
{
    [JsonPropertyName("approval_key")]
    public string ApprovalKey { get; set; }
}