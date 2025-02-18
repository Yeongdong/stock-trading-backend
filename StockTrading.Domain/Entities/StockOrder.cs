using StockTradingBackend.DataAccess.Enums;

namespace StockTradingBackend.DataAccess.Entities;

public class StockOrder
{
    public string StockCode { get; set; }
    public string TradeType { get; set; }
    public string OrderType { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public User User { get; set; }

    public StockOrder(string stockCode, string tradeType, string orderType, int quantity, decimal price, User user)
    {
        StockCode = stockCode;
        TradeType = tradeType;
        OrderType = orderType;
        Quantity = quantity;
        Price = price;
        User = user;
    }
}