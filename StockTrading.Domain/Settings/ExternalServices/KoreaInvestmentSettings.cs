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

    public KisRetrySettings RetrySettings { get; init; } = new();
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

public class KisRetrySettings
{
    [Range(0, 10)] public int MaxRetryCount { get; init; } = 3;
    [Range(100, 10000)] public int RetryDelayMs { get; init; } = 1000;
}

public class KisEndpoints
{
    // 공통
    public string TokenPath { get; init; } = "/oauth2/tokenP";
    public string WebSocketApprovalPath { get; init; } = "/oauth2/Approval";

    // 국내 주식
    public string DomesticOrderPath { get; init; } = "/uapi/domestic-stock/v1/trading/order-cash";
    public string DomesticBalancePath { get; init; } = "/uapi/domestic-stock/v1/trading/inquire-balance";
    public string DomesticOrderExecutionPath { get; init; } = "/uapi/domestic-stock/v1/trading/inquire-daily-ccld";
    public string DomesticBuyableInquiryPath { get; init; } = "/uapi/domestic-stock/v1/trading/inquire-psbl-order";
    public string DomesticCurrentPricePath { get; init; } = "/uapi/domestic-stock/v1/quotations/inquire-price";

    public string DomesticPeriodPricePath { get; init; } =
        "/uapi/domestic-stock/v1/quotations/inquire-daily-itemchartprice";

    // 해외 주식
    public string OverseasOrderPath { get; init; } = "/uapi/overseas-stock/v1/trading/order";
    public string OverseasBalancePath { get; init; } = "/uapi/overseas-stock/v1/trading/inquire-balance";
    public string OverseasOrderExecutionPath { get; init; } = "/uapi/overseas-stock/v1/trading/inquire-ccld";
    public string OverseasBuyableInquiryPath { get; init; } = "/uapi/overseas-stock/v1/trading/inquire-psamount";
    public string OverseasCurrentPricePath { get; init; } = "/uapi/overseas-price/v1/quotations/price";
    public string OverseasPeriodPricePath { get; init; } = "/uapi/overseas-price/v1/quotations/dailyprice";
}

public class KisDefaults
{
    public string AccountProductCode { get; init; } = "01";

    // 국내 주식
    public string DomesticBalanceTransactionId { get; init; } = "VTTC8434R";
    public string DomesticCurrentPriceTransactionId { get; init; } = "FHKST01010100";
    public string DomesticPeriodPriceTransactionId { get; init; } = "FHKST03010100";
    public string DomesticOrderExecutionTransactionId { get; init; } = "VTTC0081R";
    public string DomesticBuyableInquiryTransactionId { get; init; } = "VTTC8908R";

    // 해외 주식
    public string OverseasBalanceTransactionId { get; init; } = "VTRP6504R";
    public string OverseasCurrentPriceTransactionId { get; init; } = "HHDFS00000300";
    public string OverseasPeriodPriceTransactionId { get; init; } = "HHDFS76240000";
    public string OverseasOrderExecutionTransactionId { get; init; } = "VTRP6502R";
    public string OverseasBuyableInquiryTransactionId { get; init; } = "VTRP6505R";

    // 공통
    public string AfterHoursForeignPrice { get; init; } = "N";
    public string OfflineYn { get; init; } = "";
    public string InquiryDivision { get; init; } = "02";
    public string UnitPriceDivision { get; init; } = "01";
    public string FundSettlementInclude { get; init; } = "N";
    public string FinancingAmountAutoRedemption { get; init; } = "N";
    public string ProcessDivision { get; init; } = "00";
    public string SellOrderCode { get; init; } = "01";
    public string BuyOrderCode { get; init; } = "02";
    public string AllOrderCode { get; init; } = "00";
}

public class KisMarkets
{
    // 국내
    public string DomesticStock { get; init; } = "J";
    public string Kosdaq { get; init; } = "Q";

    // 해외
    public string Nasdaq { get; init; } = "NAS";
    public string Nyse { get; init; } = "NYS";
    public string Amex { get; init; } = "AMS";
}