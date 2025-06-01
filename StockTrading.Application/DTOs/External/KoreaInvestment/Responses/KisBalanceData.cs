using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 잔고조회 응답 데이터
/// </summary>
public class KisBalanceData
{
    /// <summary>
    /// 보유종목 리스트
    /// </summary>
    [JsonPropertyName("output1")]
    public List<KisPositionResponse> Positions { get; init; } = [];

    /// <summary>
    /// 계좌 요약 정보 리스트
    /// </summary>
    [JsonPropertyName("output2")]
    public List<KisAccountSummaryResponse> Summary { get; init; } = [];
}