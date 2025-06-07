using System.ComponentModel.DataAnnotations;

namespace StockTrading.Domain.Settings;

public class KoreaInvestmentSettings : IValidatableObject
{
    public const string SectionName = "KoreaInvestment";

    [Required] [Url] public string BaseUrl { get; init; } = string.Empty;

    public string AppKey { get; init; } = string.Empty;
    public string AppSecret { get; init; } = string.Empty;

    [Required] public string WebSocketUrl { get; init; } = string.Empty;

    [Range(5, 300)] public int TimeoutSeconds { get; init; } = 30;

    public RetrySettings RetrySettings { get; init; } = new();
    public KisEndpoints Endpoints { get; init; } = new();
    public KisDefaultValues DefaultValues { get; init; } = new();
    public KisMarketConstants MarketConstants { get; init; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // 운영 환경에서는 AppKey, AppSecret 필수
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment == "Production")
        {
            if (string.IsNullOrWhiteSpace(AppKey))
                results.Add(new ValidationResult(
                    "AppKey is required in production environment",
                    [nameof(AppKey)]));

            if (string.IsNullOrWhiteSpace(AppSecret))
                results.Add(new ValidationResult(
                    "AppSecret is required in production environment",
                    [nameof(AppSecret)]));
        }

        // URL 형식 검증
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            results.Add(new ValidationResult(
                "BaseUrl must be a valid absolute URL",
                [nameof(BaseUrl)]));

        if (!Uri.TryCreate(WebSocketUrl, UriKind.Absolute, out var wsUri) ||
            (wsUri.Scheme != Uri.UriSchemeWs && wsUri.Scheme != Uri.UriSchemeWss))
            results.Add(new ValidationResult(
                "WebSocketUrl must be a valid ws:// or wss:// URL",
                [nameof(WebSocketUrl)]));

        return results;
    }
}

public class RetrySettings
{
    [Range(0, 10)] public int MaxRetryCount { get; init; } = 3;

    [Range(100, 10000)] public int RetryDelayMs { get; init; } = 1000;
}

public class KisEndpoints
{
    public string TokenPath { get; init; } = "/oauth2/tokenP";
    public string WebSocketApprovalPath { get; init; } = "/oauth2/Approval";
    public string OrderPath { get; init; } = "/uapi/domestic-stock/v1/trading/order-cash";
    public string BalancePath { get; init; } = "/uapi/domestic-stock/v1/trading/inquire-balance";
    public string OrderExecutionPath { get; init; } = "/uapi/domestic-stock/v1/trading/inquire-daily-ccld";
    public string BuyableInquiryPath { get; init; } = "/uapi/domestic-stock/v1/trading/inquire-psbl-order";
    public string CurrentPricePath { get; init; } = "/uapi/domestic-stock/v1/quotations/inquire-price";
    public string PeriodPricePath { get; set; } = "/uapi/domestic-stock/v1/quotations/inquire-daily-itemchartprice";
}

public class KisDefaultValues
{
    public string AccountProductCode { get; init; } = "01";
    public string BalanceTransactionId { get; init; } = "VTTC8434R";
    public string AfterHoursForeignPrice { get; init; } = "N";
    public string OfflineYn { get; init; } = "";
    public string InquiryDivision { get; init; } = "02";
    public string UnitPriceDivision { get; init; } = "01";
    public string FundSettlementInclude { get; init; } = "N";
    public string FinancingAmountAutoRedemption { get; init; } = "N";
    public string ProcessDivision { get; init; } = "00";
    public string OrderExecutionTransactionId { get; init; } = "VTTC0081R";
    public string CurrentPriceTransactionId { get; init; } = "FHKST01010100";
    public string BuyableInquiryTransactionId { get; init; } = "VTTC8908R";
    public string SellOrderCode { get; init; } = "01";
    public string BuyOrderCode { get; init; } = "02";
    public string AllOrderCode { get; init; } = "00";
    public string PeriodPriceTransactionId { get; set; } = "FHKST03010100";
}

public class KisMarketConstants
{
    public string DomesticStock { get; init; } = "J";
    public string Kosdaq { get; init; } = "Q";
}