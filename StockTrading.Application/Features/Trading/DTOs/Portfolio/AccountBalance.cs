using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

namespace StockTrading.Application.Features.Trading.DTOs.Portfolio;

public class AccountBalance
{
    public List<KisPositionResponse> Positions { get; init; }
    public KisAccountSummaryResponse Summary { get; init; }
    public decimal DailyProfitLossAmount { get; init; }
    public decimal DailyProfitLossRate { get; init; }
}