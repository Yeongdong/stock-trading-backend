using System.Text.Json.Serialization;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;

public class AccountSummary
{
    [JsonPropertyName("dnca_tot_amt")]
    public string TotalDeposit { get; set; }
    
    [JsonPropertyName("scts_evlu_amt")]
    public string StockEvaluation { get; set; }
    
    [JsonPropertyName("tot_evlu_amt")]
    public string TotalEvaluation { get; set; }
}