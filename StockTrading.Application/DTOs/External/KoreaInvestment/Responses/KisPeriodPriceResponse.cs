using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

public class KisPeriodPriceResponse
{
    [JsonPropertyName("rt_cd")] public string ReturnCode { get; init; } = string.Empty;

    [JsonPropertyName("msg_cd")] public string MessageCode { get; init; } = string.Empty;

    [JsonPropertyName("msg1")] public string Message { get; init; } = string.Empty;

    [JsonPropertyName("output1")] public KisPeriodPriceOutput1? Output1 { get; init; }

    [JsonPropertyName("output2")] public List<KisPeriodPriceOutput2> Output2 { get; init; } = [];

    [JsonIgnore] public bool IsSuccess => ReturnCode == "0";

    [JsonIgnore] public bool HasData => Output1 != null && Output2.Count > 0;

    [JsonIgnore] public KisPeriodPriceOutput1? CurrentInfo => Output1;

    [JsonIgnore] public List<KisPeriodPriceOutput2> PriceData => Output2;

    [JsonIgnore] public bool HasPriceData => Output2.Count > 0;
}