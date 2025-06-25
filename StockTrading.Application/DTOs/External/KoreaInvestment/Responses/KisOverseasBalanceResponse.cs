namespace StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

/// <summary>
/// KIS API 해외 잔고 조회 응답
/// </summary>
public class KisOverseasBalanceResponse : KisBaseResponse<List<KisOverseasBalanceData>>
{
    /// <summary>
    /// 해외 주식 포지션 목록
    /// </summary>
    public List<KisOverseasBalanceData> Positions => Output ?? [];

    /// <summary>
    /// 포지션 데이터 존재 여부
    /// </summary>
    public bool HasPositions => Positions.Count != 0;
}