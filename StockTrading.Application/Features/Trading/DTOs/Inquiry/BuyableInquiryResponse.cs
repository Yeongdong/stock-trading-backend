namespace StockTrading.Application.Features.Trading.DTOs.Inquiry;

public class BuyableInquiryResponse
{
    public string StockCode { get; init; } = string.Empty;
    public string StockName { get; init; } = string.Empty;
    public decimal BuyableAmount { get; init; }
    public int BuyableQuantity { get; init; }
    public decimal OrderableAmount { get; init; }
    public decimal CashBalance { get; init; }
    public decimal OrderPrice { get; init; }
    public decimal CurrentPrice { get; init; }
    public int UnitQuantity { get; init; } = 1;
}