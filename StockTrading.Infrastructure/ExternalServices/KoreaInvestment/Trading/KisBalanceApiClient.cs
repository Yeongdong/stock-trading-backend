using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Trading.DTOs.Portfolio;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Settings.ExternalServices;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.RealTime.Converters;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Trading;

public class KisBalanceApiClient : KisApiClientBase, IKisBalanceApiClient
{
    private readonly StockDataConverter _stockDataConverter;

    public KisBalanceApiClient(HttpClient httpClient, StockDataConverter stockDataConverter,
        IOptions<KoreaInvestmentSettings> settings, ILogger<KisBalanceApiClient> logger) : base(httpClient, settings,
        logger)
    {
        _stockDataConverter = stockDataConverter;
    }

    #region 국내 주식

    public async Task<AccountBalance> GetStockBalanceAsync(UserInfo user)
    {
        var queryParams = CreateBalanceQueryParams(user);
        var httpRequest = CreateBalanceHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var kisResponse = await response.Content.ReadFromJsonAsync<KisBalanceResponse>();

        ValidateBalanceResponse(kisResponse);

        return CreateAccountBalance(kisResponse);
    }

    public async Task<BuyableInquiryResponse> GetBuyableInquiryAsync(BuyableInquiryRequest request, UserInfo user)
    {
        var queryParams = CreateBuyableInquiryQueryParams(request, user);
        var httpRequest = CreateBuyableInquiryHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var kisResponse = await response.Content.ReadFromJsonAsync<KisBuyableInquiryResponse>();

        ValidateBuyableInquiryResponse(kisResponse);

        return _stockDataConverter.ConvertToBuyableInquiryResponse(kisResponse.Output, request.OrderPrice,
            request.StockCode);
    }

    #endregion

    #region 해외 주식

    public async Task<OverseasAccountBalance> GetOverseasStockBalanceAsync(UserInfo user)
    {
        var queryParams = CreateOverseasBalanceQueryParams(user);
        var httpRequest = CreateOverseasBalanceHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var kisResponse = await response.Content.ReadFromJsonAsync<KisOverseasBalanceResponse>();

        ValidateOverseasBalanceResponse(kisResponse);

        return CreateOverseasAccountBalance(kisResponse);
    }

    #endregion

    #region 국내 주식 Private Methods

    private Dictionary<string, string> CreateBalanceQueryParams(UserInfo user)
    {
        var defaults = _settings.DefaultValues;
        return new Dictionary<string, string>
        {
            ["CANO"] = user.AccountNumber,
            ["ACNT_PRDT_CD"] = defaults.AccountProductCode,
            ["AFHR_FLPR_YN"] = defaults.AfterHoursForeignPrice,
            ["OFL_YN"] = defaults.OfflineYn,
            ["INQR_DVSN"] = defaults.InquiryDivision,
            ["UNPR_DVSN"] = defaults.UnitPriceDivision,
            ["FUND_STTL_ICLD_YN"] = defaults.FundSettlementInclude,
            ["FNCG_AMT_AUTO_RDPT_YN"] = defaults.FinancingAmountAutoRedemption,
            ["PRCS_DVSN"] = defaults.ProcessDivision,
            ["CTX_AREA_FK100"] = "",
            ["CTX_AREA_NK100"] = ""
        };
    }

    private HttpRequestMessage CreateBalanceHttpRequest(Dictionary<string, string> queryParams, UserInfo user)
    {
        var url = BuildGetUrl(_settings.Endpoints.DomesticBalancePath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.DefaultValues.DomesticBalanceTransactionId, user);
        return httpRequest;
    }

    private static void ValidateBalanceResponse(KisBalanceResponse? kisResponse)
    {
        if (!kisResponse.IsSuccess)
            throw new Exception($"잔고조회 실패: {kisResponse.Message}");

        if (!kisResponse.HasData)
            throw new Exception("잔고조회 데이터가 없습니다.");
    }

    private static AccountBalance CreateAccountBalance(KisBalanceResponse? kisResponse)
    {
        return new AccountBalance
        {
            Positions = kisResponse.Positions,
            Summary = kisResponse.Summary.FirstOrDefault() ?? new KisAccountSummaryResponse()
        };
    }

    private Dictionary<string, string> CreateBuyableInquiryQueryParams(BuyableInquiryRequest request, UserInfo user)
    {
        var defaults = _settings.DefaultValues;
        return new Dictionary<string, string>
        {
            ["CANO"] = user.AccountNumber,
            ["ACNT_PRDT_CD"] = defaults.AccountProductCode,
            ["PDNO"] = request.StockCode,
            ["ORD_UNPR"] = request.OrderPrice.ToString("F0"),
            ["ORD_DVSN"] = request.OrderType,
            ["CMA_EVLU_AMT_ICLD_YN"] = "Y",
            ["OVRS_ICLD_YN"] = "N"
        };
    }

    private HttpRequestMessage CreateBuyableInquiryHttpRequest(Dictionary<string, string> queryParams, UserInfo user)
    {
        var url = BuildGetUrl(_settings.Endpoints.DomesticBuyableInquiryPath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.DefaultValues.DomesticBuyableInquiryTransactionId, user);
        return httpRequest;
    }

    private void ValidateBuyableInquiryResponse(KisBuyableInquiryResponse? kisResponse)
    {
        if (!kisResponse.IsSuccess)
            throw new Exception($"매수가능조회 실패: {kisResponse.Message}");

        if (!kisResponse.HasData)
            throw new Exception("매수가능조회 데이터가 없습니다.");
    }

    #endregion

    #region 해외 주식 Private Methods

    private Dictionary<string, string> CreateOverseasBalanceQueryParams(UserInfo user)
    {
        var defaults = _settings.DefaultValues;
        return new Dictionary<string, string>
        {
            ["CANO"] = user.AccountNumber,
            ["ACNT_PRDT_CD"] = defaults.AccountProductCode,
            ["OVRS_EXCG_CD"] = "",
            ["TR_CRCY_CD"] = "",
            ["CTX_AREA_FK200"] = "",
            ["CTX_AREA_NK200"] = ""
        };
    }

    private HttpRequestMessage CreateOverseasBalanceHttpRequest(Dictionary<string, string> queryParams, UserInfo user)
    {
        var url = BuildGetUrl(_settings.Endpoints.OverseasBalancePath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.DefaultValues.OverseasBalanceTransactionId, user);
        return httpRequest;
    }

    private static void ValidateOverseasBalanceResponse(KisOverseasBalanceResponse? kisResponse)
    {
        if (!kisResponse.IsSuccess)
            throw new Exception($"해외 잔고조회 실패: {kisResponse.Message}");

        if (!kisResponse.HasData)
            throw new Exception("해외 잔고조회 데이터가 없습니다.");
    }

    private static OverseasAccountBalance CreateOverseasAccountBalance(KisOverseasBalanceResponse? kisResponse)
    {
        return new OverseasAccountBalance
        {
            Positions = kisResponse.Positions
        };
    }

    #endregion
}