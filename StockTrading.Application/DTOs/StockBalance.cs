using StockTrading.DataAccess.DTOs;

namespace stock_trading_backend.DTOs;

public class StockBalance 
{
    public List<Position> Positions { get; set; }
    public Summary Summary { get; set; }
}