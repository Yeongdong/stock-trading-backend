using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 주문체결조회 응답 데이터
/// </summary>
public class KisOrderExecutionData
{
    /// <summary>
    /// 주문체결내역 리스트
    /// </summary>
    [JsonPropertyName("output1")]
    public List<KisOrderExecutionItem> ExecutionItems { get; init; } = [];

    /// <summary>
    /// 연속조회검색조건100
    /// </summary>
    [JsonPropertyName("ctx_area_fk100")]
    public string CtxAreaFk100 { get; init; } = string.Empty;

    /// <summary>
    /// 연속조회키100
    /// </summary>
    [JsonPropertyName("ctx_area_nk100")]
    public string CtxAreaNk100 { get; init; } = string.Empty;
}