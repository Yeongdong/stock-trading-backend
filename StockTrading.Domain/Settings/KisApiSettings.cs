namespace StockTrading.Domain.Settings;

public class KisApiSettings
{
    public const string SectionName = "KoreaInvestment";

    public string BaseUrl { get; init; } = string.Empty;
    public string AppKey { get; init; } = string.Empty;
    public string AppSecret { get; init; } = string.Empty;
    public string WebSocketUrl { get; init; } = "ws://ops.koreainvestment.com:31000";

    /// <summary>
    /// API 엔드포인트들
    /// </summary>
    public ApiEndpoints Endpoints { get; init; } = new();

    /// <summary>
    /// 기본 설정값들
    /// </summary>
    public DefaultValues Defaults { get; init; } = new();
}