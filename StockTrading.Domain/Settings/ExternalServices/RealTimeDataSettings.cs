using System.ComponentModel.DataAnnotations;
using StockTrading.Domain.Settings.Common;

namespace StockTrading.Domain.Settings.ExternalServices;

public class RealTimeDataSettings : BaseSettings
{
    public const string SectionName = "RealTimeData";

    public WebSocketConfig WebSocket { get; init; } = new();
    public MessageTypes MessageTypes { get; init; } = new();
    public ParsingConfig Parsing { get; init; } = new();
    public SubscriptionConfig Subscription { get; init; } = new();

    protected override IEnumerable<ValidationResult> ValidateEnvironmentSpecific(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // WebSocket 설정 검증
        if (WebSocket.BufferSize is < 1024 or > 1024 * 1024)
            results.Add(new ValidationResult("WebSocket BufferSize must be between 1KB and 1MB",
                [$"{nameof(WebSocket)}.{nameof(WebSocket.BufferSize)}"]));

        // Parsing 설정 검증
        if (Parsing.StockCodeLength != 6)
            results.Add(new ValidationResult("StockCodeLength must be 6 for Korean stocks",
                [$"{nameof(Parsing)}.{nameof(Parsing.StockCodeLength)}"]));

        return results;
    }
}

public class WebSocketConfig
{
    [Range(5, 300)] public int ConnectionTimeoutSeconds { get; init; } = 30;

    [Range(10, 300)] public int KeepAliveIntervalSeconds { get; init; } = 30;

    [Range(1024, 1048576)] public int BufferSize { get; init; } = 4096;

    [Range(1000, 30000)] public int ReconnectDelayMs { get; init; } = 3000;

    [Range(100, 5000)] public int SubscriptionDelayMs { get; init; } = 500;
}

public class MessageTypes
{
    public string StockAskBid { get; init; } = "H0STASP0";
    public string StockExecution { get; init; } = "H0STCNT0";
    public string TradeNotification { get; init; } = "H0STCNI0";
    public string TradeNotificationDemo { get; init; } = "H0STCNI9";
    public string PingPong { get; init; } = "PINGPONG";

    public static class ChangeSign
    {
        public static readonly string[] RiseCodes = { "1", "2" };
        public static readonly string[] FallCodes = { "4", "5" };
    }

    public static class ChangeType
    {
        public const string Rise = "상승";
        public const string Fall = "하락";
        public const string Flat = "보합";
    }
}

public class ParsingConfig
{
    public string PipeDelimiter { get; init; } = "|";
    public string FieldDelimiter { get; init; } = "^";

    [Range(2, 10)] public int MinimumPipeSegments { get; init; } = 4;

    [Range(5, 50)] public int MinimumFieldsForProcessing { get; init; } = 15;

    [Range(4, 8)] public int StockCodeLength { get; init; } = 6;

    [Range(4, 8)] public int TradeTimeLength { get; init; } = 6;

    public static class FieldIndices
    {
        public const int StockCode = 0;
        public const int TradeTime = 1;
        public const int CurrentPrice = 2;
        public const int ChangeSign = 3;
        public const int PriceChange = 4;
        public const int ChangeRate = 5;
        public const int WeightedAvgPrice = 6;
        public const int OpenPrice = 7;
        public const int HighPrice = 8;
        public const int LowPrice = 9;
        public const int AskPrice1 = 10;
        public const int BidPrice1 = 11;
        public const int Volume = 12;
        public const int TotalVolume = 13;
        public const int TotalTradeAmount = 14;
    }

    public static class ValidationConstants
    {
        public static class Date
        {
            public const string Format = "yyyyMMdd";
            public const int MaxInquiryDays = 31;
        }

        public static class StockCode
        {
            public const int Length = 6;
            public const string Pattern = @"^\d{6}$";
        }
    }
}

public class SubscriptionConfig
{
    public string Register { get; init; } = "1";
    public string Unregister { get; init; } = "2";
}