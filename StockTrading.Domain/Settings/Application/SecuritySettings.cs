using System.ComponentModel.DataAnnotations;
using StockTrading.Domain.Settings.Common;

namespace StockTrading.Domain.Settings.Application;

public class SecuritySettings : BaseSettings
{
    public const string SectionName = "Security";

    public CorsSettings Cors { get; init; } = new();
    public HeaderSettings Headers { get; init; } = new();
}

public class CorsSettings
{
    [Range(0, 86400)] // 0 ~ 24시간
    public int PreflightMaxAgeSeconds { get; init; } = 86400;

    public List<string> ExposedHeaders { get; init; } =
    [
        "Connection",
        "Upgrade"
    ];
}

public class HeaderSettings
{
    public string XContentTypeOptions { get; init; } = "nosniff";
    public string XFrameOptions { get; init; } = "DENY";
    public string XXSSProtection { get; init; } = "1; mode=block";
    public string ReferrerPolicy { get; init; } = "strict-origin-when-cross-origin";
    public string ContentSecurityPolicy { get; init; } = "default-src 'self'";
}

public class SignalRSettings : BaseSettings
{
    public const string SectionName = "SignalR";

    [Required] public string HubPath { get; init; } = "/stockhub";

    public List<string> Transports { get; init; } =
    [
        "WebSockets",
        "ServerSentEvents"
    ];

    public bool CloseOnAuthenticationExpiration { get; init; } = false;
    public BufferSettings BufferSizes { get; init; } = new();
}

public class BufferSettings
{
    [Range(1024, 1048576)] // 1KB ~ 1MB
    public int ApplicationMaxBufferSize { get; init; } = 65536;

    [Range(1024, 1048576)] // 1KB ~ 1MB
    public int TransportMaxBufferSize { get; init; } = 65536;
}