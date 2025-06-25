using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

namespace StockTrading.Application.Features.Trading.DTOs.Portfolio;

/// <summary>
/// 해외 주식 계좌 잔고
/// </summary>
public class OverseasAccountBalance
{
    /// <summary>
    /// 해외 주식 포지션 목록
    /// </summary>
    public List<KisOverseasBalanceData> Positions { get; init; } = [];

    /// <summary>
    /// 포지션 보유 여부
    /// </summary>
    public bool HasPositions => Positions.Any();

    /// <summary>
    /// 총 포지션 수
    /// </summary>
    public int TotalPositions => Positions.Count;
}