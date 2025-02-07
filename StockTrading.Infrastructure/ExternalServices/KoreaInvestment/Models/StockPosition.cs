using System.Text.Json.Serialization;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;

public class StockPosition
{
    [JsonPropertyName("pdno")]
    public string StockCode { get; set; }
    
    [JsonPropertyName("prdt_name")]
    public string StockName { get; set; }
    
    [JsonPropertyName("hldg_qty")]
    public string Quantity { get; set; }
    
    [JsonPropertyName("pchs_avg_pric")]
    public string AveragePrice { get; set; }
    
    [JsonPropertyName("prpr")]
    public string CurrentPrice { get; set; }
    
    [JsonPropertyName("evlu_pfls_amt")]
    public string ProfitLoss { get; set; }
    
    [JsonPropertyName("evlu_pfls_rt")]
    public string ProfitLossRate { get; set; }
}