using System.ComponentModel.DataAnnotations;
using StockTrading.Domain.Settings.Common;

namespace StockTrading.Domain.Settings.ExternalServices;

public class KoreaInvestmentSettings : BaseSettings
{
    public const string SectionName = "KoreaInvestment";

    [Required] public string BaseUrl { get; init; } = string.Empty;

    public string AppKey { get; init; } = string.Empty;
    public string AppSecret { get; init; } = string.Empty;

    [Required] public string WebSocketUrl { get; init; } = string.Empty;

    [Range(5, 300)] public int TimeoutSeconds { get; init; } = 30;

    public RetrySettings RetrySettings { get; init; } = new();
    public KisEndpoints Endpoints { get; init; } = new();
    public KisDefaults DefaultValues { get; init; } = new();
    public KisMarkets MarketConstants { get; init; } = new();

    protected override IEnumerable<ValidationResult> ValidateEnvironmentSpecific(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // 운영 환경에서 필수 필드 검증
        var appKeyValidation = ValidateRequiredInProduction(AppKey, nameof(AppKey));
        if (appKeyValidation != null) results.Add(appKeyValidation);

        var appSecretValidation = ValidateRequiredInProduction(AppSecret, nameof(AppSecret));
        if (appSecretValidation != null) results.Add(appSecretValidation);

        // URL 형식 검증
        var baseUrlValidation = ValidateUrl(BaseUrl, nameof(BaseUrl));
        if (baseUrlValidation != null) results.Add(baseUrlValidation);

        // WebSocket URL 검증
        if (!Uri.TryCreate(WebSocketUrl, UriKind.Absolute, out var wsUri) ||
            (wsUri.Scheme != Uri.UriSchemeWs && wsUri.Scheme != Uri.UriSchemeWss))
            results.Add(
                new ValidationResult("WebSocketUrl must be a valid ws:// or wss:// URL", [nameof(WebSocketUrl)]));

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
    public string PeriodPricePath { get; init; } = "/uapi/domestic-stock/v1/quotations/inquire-daily-itemchartprice";
}

public class KisDefaults
{
    public string AccountProductCode { get; init; } = "01";
    public string BalanceTransactionId { get; init; } = "VTTC8434R";
    public string PeriodPriceTransactionId { get; init; } = "FHKST03010100";
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
}

public class KisMarkets
{
    public string DomesticStock { get; init; } = "J";
    public string Kosdaq { get; init; } = "Q";
}