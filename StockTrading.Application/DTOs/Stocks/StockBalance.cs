namespace StockTrading.Application.DTOs.Stocks;

public class StockBalance 
{
    public List<Position> Positions { get; set; }
    public Summary Summary { get; set; }
}