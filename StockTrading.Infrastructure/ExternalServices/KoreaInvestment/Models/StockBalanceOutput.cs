using System.Text.Json.Serialization;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;

public class StockBalanceOutput
{
    [JsonPropertyName("rt_cd")]
    public string ReturnCode { get; set; }
    
    [JsonPropertyName("msg_cd")]
    public string MessageCode { get; set; }
    
    [JsonPropertyName("msg1")]
    public string Message { get; set; }
    
    [JsonPropertyName("output1")]
    public List<StockPosition> Positions { get; set; }
    
    [JsonPropertyName("output2")]
    public List<AccountSummary> Summary { get; set; }
}