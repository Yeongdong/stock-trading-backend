using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

public class KisBalanceResponse : KisBaseResponse<KisBalanceData>
{
    [JsonPropertyName("rt_cd")] public string ReturnCode { get; set; }

    [JsonPropertyName("msg_cd")] public string MessageCode { get; set; }

    [JsonPropertyName("msg1")] public string Message { get; set; }

    [JsonPropertyName("output1")] public List<KisPositionResponse> Positions { get; init; } = [];

    [JsonPropertyName("output2")] public List<KisAccountSummaryResponse> Summary { get; init; } = [];

    [JsonIgnore] public bool IsSuccess => ReturnCode == "0";

    [JsonIgnore] public bool HasData => Positions.Count > 0 || Summary.Count > 0;
}