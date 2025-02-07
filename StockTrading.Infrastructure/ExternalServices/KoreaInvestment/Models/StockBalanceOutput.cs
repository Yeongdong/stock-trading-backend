using System.Text.Json.Serialization;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;

namespace stock_trading_backend.DTOs;

public class StockBalanceOutput
{
    [JsonPropertyName("output1")]
    public List<StockPosition> Positions { get; set; }
    
    [JsonPropertyName("output2")]
    public AccountSummary Summary { get; set; }
}