using System.ComponentModel;

namespace StockTrading.Domain.Enums;

public enum OrderType
{
    [Description("00")] FixedPrice,     // 지정가
    [Description("01")] MarketPrice,    // 시장가
}