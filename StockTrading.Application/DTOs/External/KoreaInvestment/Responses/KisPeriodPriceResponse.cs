using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

public class KisPeriodPriceResponse : KisBaseResponse<KisPeriodPriceOutputData>
{
}

public class KisPeriodPriceOutputData
{
    [JsonPropertyName("output1")]
    public KisPeriodPriceOutput1? Summary { get; init; }
    [JsonPropertyName("output2")]
    public List<KisPeriodPriceData> PriceData { get; init; } = [];
    public bool HasData => PriceData.Count != 0;
}