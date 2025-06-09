namespace StockTrading.Application.Features.Trading.DTOs.Inquiry;

public class KisCurrentPriceResponse
{
    public string StockCode { get; init; } = string.Empty;
    public string StockName { get; init; } = string.Empty;
    public decimal CurrentPrice { get; init; }
    public decimal PriceChange { get; init; }
    public decimal ChangeRate { get; init; }
    public string ChangeType { get; init; } = string.Empty;
    public decimal OpenPrice { get; init; }
    public decimal HighPrice { get; init; }
    public decimal LowPrice { get; init; }
    public long Volume { get; init; }
    public DateTime InquiryTime { get; init; } = DateTime.Now;
}