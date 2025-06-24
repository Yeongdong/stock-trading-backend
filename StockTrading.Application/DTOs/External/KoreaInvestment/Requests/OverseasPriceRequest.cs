namespace StockTrading.Application.DTOs.External.KoreaInvestment.Requests;

public class OverseasPriceRequest
{
    public string StockCode { get; init; } = string.Empty;
    public string MarketCode { get; init; } = string.Empty;
}