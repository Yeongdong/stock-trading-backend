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
        var url = BuildGetUrl(_settings.Endpoints.BalancePath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.DefaultValues.BalanceTransactionId, user);
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
        var url = BuildGetUrl(_settings.Endpoints.BuyableInquiryPath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.DefaultValues.BuyableInquiryTransactionId, user);
        return httpRequest;
    }

    private void ValidateBuyableInquiryResponse(KisBuyableInquiryResponse? kisResponse)
    {
        if (!kisResponse.IsSuccess)
            throw new Exception($"매수가능조회 실패: {kisResponse.Message}");

        if (!kisResponse.HasData)
            throw new Exception("매수가능조회 데이터가 없습니다.");
    }
}