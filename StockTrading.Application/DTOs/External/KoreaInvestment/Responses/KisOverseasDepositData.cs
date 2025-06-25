using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

public class KisOverseasDepositData
{
    [JsonPropertyName("ovrs_buy_able_amt")]
    public string OrderableAmount { get; init; } = string.Empty;

    [JsonPropertyName("frcr_buy_able_amt1")]
    public string DepositAmount { get; init; } = string.Empty;

    [JsonPropertyName("exrt")] public string ExchangeRate { get; init; } = string.Empty;

    [JsonPropertyName("tr_crcy_cd")] public string CurrencyCode { get; init; } = string.Empty;
}