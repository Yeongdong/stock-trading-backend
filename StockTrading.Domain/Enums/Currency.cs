using System.ComponentModel;

namespace StockTrading.Domain.Enums;

public enum Currency
{
    [Description("KRW")] Krw,
    [Description("USD")] Usd,
    [Description("JPY")] Jpy,
    [Description("GBP")] Gbp,
    [Description("HKD")] Hkd
}