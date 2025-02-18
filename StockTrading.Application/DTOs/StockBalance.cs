namespace StockTrading.DataAccess.DTOs;

public class StockBalance 
{
    public List<Position> Positions { get; set; }
    public Summary Summary { get; set; }
}