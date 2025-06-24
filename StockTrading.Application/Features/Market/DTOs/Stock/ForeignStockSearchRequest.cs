namespace StockTrading.Application.Features.Market.DTOs.Stock;

public class ForeignStockSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public string? Exchange { get; set; }
    public int Limit { get; set; } = 50;
}