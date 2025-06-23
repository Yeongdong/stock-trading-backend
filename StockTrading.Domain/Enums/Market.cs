using System.ComponentModel;

namespace StockTrading.Domain.Enums;

public enum Market
{
    // 국내 시장
    [Description("KOSPI")] Kospi,
    [Description("KOSDAQ")] Kosdaq,
    [Description("KONEX")] Konex,

    // 해외 시장 - 미국
    [Description("NYSE")] Nyse,
    [Description("NASDAQ")] Nasdaq,

    // 해외 시장 - 기타
    [Description("TSE")] Tokyo,
    [Description("LSE")] London,
    [Description("HKSE")] HongKong
}