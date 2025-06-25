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
        var allPositions = new List<KisOverseasBalanceData>();
        OverseasDepositInfo? depositInfo = null;

        var exchangeCurrencyPairs = new[]
        {
            ("NASD", "USD"), // 나스닥
            ("NYSE", "USD"), // 뉴욕증권거래소  
            ("AMEX", "USD"), // 아멕스
            ("SEHK", "HKD"), // 홍콩
            ("TKSE", "JPY"), // 일본
        };

        foreach (var (exchangeCode, currencyCode) in exchangeCurrencyPairs)
        {
            var queryParams = CreateOverseasBalanceQueryParams(user, exchangeCode, currencyCode);
            var httpRequest = CreateOverseasBalanceHttpRequest(queryParams, user);

            var response = await _httpClient.SendAsync(httpRequest);
            var kisResponse = await response.Content.ReadFromJsonAsync<KisOverseasBalanceResponse>();

            if (kisResponse?.IsSuccess != true) continue;

            if (kisResponse.HasPositions)
            {
                var validPositions = kisResponse.Positions
                    .Where(p => !string.IsNullOrEmpty(p.StockCode) && !string.IsNullOrEmpty(p.StockName))
                    .ToList();

                allPositions.AddRange(validPositions);
            }

            // 예수금 정보는 첫 번째 성공한 응답에서만 수집 
            depositInfo ??= ConvertToDepositInfo(kisResponse.DepositData, currencyCode);
        }

        return new OverseasAccountBalance
        {
            Positions = allPositions,
            DepositInfo = depositInfo ?? new OverseasDepositInfo()
        };
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

    private Dictionary<string, string> CreateOverseasBalanceQueryParams(UserInfo user, string exchangeCode,
        string currencyCode)
    {
        var defaults = _settings.DefaultValues;
        return new Dictionary<string, string>
        {
            ["CANO"] = user.AccountNumber,
            ["ACNT_PRDT_CD"] = defaults.AccountProductCode,
            ["OVRS_EXCG_CD"] = exchangeCode, // 해외거래소코드 (필수)
            ["TR_CRCY_CD"] = currencyCode, // 거래통화코드 (필수)
            ["CTX_AREA_FK200"] = "", // 연속조회검색조건200 (첫 조회시 공백)
            ["CTX_AREA_NK200"] = "" // 연속조회키200 (첫 조회시 공백)
        };
    }

    private HttpRequestMessage CreateOverseasBalanceHttpRequest(Dictionary<string, string> queryParams, UserInfo user)
    {
        var url = BuildGetUrl(_settings.Endpoints.OverseasBalancePath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.DefaultValues.OverseasBalanceTransactionId, user);
        return httpRequest;
    }

    private static OverseasDepositInfo ConvertToDepositInfo(KisOverseasDepositData? data, string currencyCode)
    {
        if (data == null) return new OverseasDepositInfo { CurrencyCode = currencyCode };

        return new OverseasDepositInfo
        {
            TotalDepositAmount = decimal.TryParse(data.DepositAmount, out var deposit) ? deposit : 0,
            OrderableAmount = decimal.TryParse(data.OrderableAmount, out var orderable) ? orderable : 0,
            CurrencyCode = currencyCode,
            ExchangeRate = decimal.TryParse(data.ExchangeRate, out var rate) ? rate : 0,
            InquiryTime = DateTime.Now
        };
    }

    #endregion
}