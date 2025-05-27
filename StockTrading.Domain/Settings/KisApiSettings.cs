namespace StockTrading.Domain.Settings;

public class KisApiSettings
{
    public const string SectionName = "KoreaInvestment";

    public string BaseUrl { get; set; } = string.Empty;
    public string AppKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string WebSocketUrl { get; set; } = "ws://ops.koreainvestment.com:31000";

    /// <summary>
    /// API 엔드포인트들
    /// </summary>
    public ApiEndpoints Endpoints { get; set; } = new();

    /// <summary>
    /// 기본 설정값들
    /// </summary>
    public DefaultValues Defaults { get; set; } = new();
}