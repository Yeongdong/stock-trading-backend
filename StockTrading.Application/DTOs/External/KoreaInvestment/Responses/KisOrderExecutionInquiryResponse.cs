using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 원본 응답용 DTO
/// </summary>
public class KisOrderExecutionInquiryResponse : KisBaseResponse<KisOrderExecutionData>
{
    [JsonPropertyName("rt_cd")] public string ReturnCode { get; init; } = string.Empty;

    [JsonPropertyName("msg_cd")] public string MessageCode { get; init; } = string.Empty;

    [JsonPropertyName("msg1")] public string Message { get; init; } = string.Empty;

    [JsonPropertyName("output1")] public List<KisOrderExecutionItem> ExecutionItems { get; init; } = [];

    [JsonPropertyName("ctx_area_fk100")] public string CtxAreaFk100 { get; init; } = string.Empty;

    [JsonPropertyName("ctx_area_nk100")] public string CtxAreaNk100 { get; init; } = string.Empty;

    [JsonIgnore] public bool IsSuccess => ReturnCode == "0";

    [JsonIgnore] public bool HasData => ExecutionItems.Count > 0;
}