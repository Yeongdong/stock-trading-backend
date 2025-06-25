using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 해외 잔고 조회 응답
/// </summary>
public class KisOverseasBalanceResponse : KisBaseResponse<List<KisOverseasBalanceData>>
{
    /// <summary>
    /// 해외 주식 포지션 목록 (output1)
    /// </summary>
    public List<KisOverseasBalanceData> Positions => Output ?? [];
    
    /// <summary>
    /// 해외 주식 예수금 정보 (output2)
    /// </summary>
    [JsonPropertyName("output2")]
    public KisOverseasDepositData? DepositData { get; init; }

    /// <summary>
    /// 포지션 데이터 존재 여부
    /// </summary>
    public bool HasPositions => Positions.Any(p => !string.IsNullOrEmpty(p.StockCode));
}

