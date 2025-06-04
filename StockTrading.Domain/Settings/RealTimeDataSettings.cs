using System.ComponentModel.DataAnnotations;

namespace StockTrading.Domain.Settings;

public class RealTimeDataSettings : IValidatableObject
{
    public const string SectionName = "RealTimeData";

    public WebSocketSettings WebSocket { get; init; } = new();
    public MessageTypeSettings MessageTypes { get; init; } = new();
    public ParsingSettings Parsing { get; init; } = new();
    public SubscriptionSettings Subscription { get; init; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // WebSocket 설정 검증
        if (WebSocket.BufferSize is < 1024 or > 1024 * 1024)
        {
            results.Add(new ValidationResult(
                "WebSocket BufferSize must be between 1KB and 1MB",
                [$"{nameof(WebSocket)}.{nameof(WebSocket.BufferSize)}"]));
        }

        // Parsing 설정 검증
        if (Parsing.StockCodeLength != 6)
        {
            results.Add(new ValidationResult(
                "StockCodeLength must be 6 for Korean stocks",
                [$"{nameof(Parsing)}.{nameof(Parsing.StockCodeLength)}"]));
        }

        return results;
    }
}

public class WebSocketSettings
{
    [Range(5, 300)] public int ConnectionTimeoutSeconds { get; init; } = 30;

    [Range(10, 300)] public int KeepAliveIntervalSeconds { get; init; } = 30;

    [Range(1024, 1048576)] // 1KB ~ 1MB
    public int BufferSize { get; init; } = 4096;

    [Range(1000, 30000)] public int ReconnectDelayMs { get; init; } = 3000;

    [Range(100, 5000)] public int SubscriptionDelayMs { get; init; } = 500;
}

public class MessageTypeSettings
{
    public string StockAskBid { get; init; } = "H0STASP0";
    public string StockExecution { get; init; } = "H0STCNT0";
    public string TradeNotification { get; init; } = "H0STCNI0";
    public string TradeNotificationDemo { get; init; } = "H0STCNI9";
    public string PingPong { get; init; } = "PINGPONG";
}

public class ParsingSettings
{
    public string PipeDelimiter { get; init; } = "|";
    public string FieldDelimiter { get; init; } = "^";

    [Range(2, 10)] public int MinimumPipeSegments { get; init; } = 4;

    [Range(5, 50)] public int MinimumFieldsForProcessing { get; init; } = 15;

    [Range(4, 8)] public int StockCodeLength { get; init; } = 6;

    [Range(4, 8)] public int TradeTimeLength { get; init; } = 6;
}

public class SubscriptionSettings
{
    public string Register { get; init; } = "1";
    public string Unregister { get; init; } = "2";
}