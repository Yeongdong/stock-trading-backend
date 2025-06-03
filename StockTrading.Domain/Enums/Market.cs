using System.ComponentModel;

namespace StockTrading.Domain.Enums;

public enum Market
{
    [Description("KOSPI")] Kospi,

    [Description("KOSDAQ")] Kosdaq,

    [Description("KONEX")] Konex
}