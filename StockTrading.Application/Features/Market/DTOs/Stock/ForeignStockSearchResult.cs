namespace StockTrading.Application.Features.Market.DTOs.Stock;

public class ForeignStockSearchResult
{
    public List<ForeignStockInfo> Stocks { get; set; } = new();
    public int Count { get; set; }
}

public class ForeignStockInfo
{
    public string Symbol { get; set; } = string.Empty;
    public string DisplaySymbol { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}