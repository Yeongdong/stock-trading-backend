using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.External.KoreaInvestment.Requests;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Trading.Portfolio;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Services;
using StockTrading.Domain.Settings;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Converters;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/**
 * KIS와의 통신을 담당하는 클래스
 */
public class KisApiClient : IKisApiClient
{
    private readonly HttpClient _httpClient;
    private readonly KoreaInvestmentSettings _settings;
    private readonly ILogger<KisApiClient> _logger;
    private readonly StockDataConverter _converter;

    public KisApiClient(HttpClient httpClient, IOptions<KoreaInvestmentSettings> settings, ILogger<KisApiClient> logger, StockDataConverter converter)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _converter = converter;
    }

    public async Task<OrderResponse> PlaceOrderAsync(OrderRequest request, UserInfo user)
    {
        var kisRequest = CreateKisOrderRequest(request, user);
        var httpRequest = CreateOrderHttpRequest(kisRequest, request.tr_id, user);

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        return await ProcessOrderResponse(response);
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

    public async Task<OrderExecutionInquiryResponse> GetOrderExecutionsAsync(OrderExecutionInquiryRequest request, UserInfo user)
    {
        var queryParams = CreateOrderExecutionQueryParams(request, user);
        var httpRequest = CreateOrderExecutionHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var responseContent = await ValidateAndReadResponse(response);

        var kisResponse = JsonSerializer.Deserialize<KisOrderExecutionInquiryResponse>(responseContent);
        ValidateOrderExecutionResponse(kisResponse);

        return _converter.ConvertToOrderExecutionResponse(kisResponse);
    }

    public async Task<BuyableInquiryResponse> GetBuyableInquiryAsync(BuyableInquiryRequest request, UserInfo user)
    {
        var defaults = _settings.DefaultValues;
        var queryParams = new Dictionary<string, string>
        {
            ["CANO"] = user.AccountNumber,
            ["ACNT_PRDT_CD"] = defaults.AccountProductCode,
            ["PDNO"] = request.StockCode,
            ["ORD_UNPR"] = request.OrderPrice.ToString("F0"),
            ["ORD_DVSN"] = request.OrderType,
            ["CMA_EVLU_AMT_ICLD_YN"] = "Y",
            ["OVRS_ICLD_YN"] = "N"
        };

        var url = BuildGetUrl(_settings.Endpoints.BuyableInquiryPath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, defaults.BuyableInquiryTransactionId, user);

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();
        var kisResponse = await response.Content.ReadFromJsonAsync<KisBuyableInquiryResponse>();

        if (!kisResponse.IsSuccess)
            throw new Exception($"매수가능조회 실패: {kisResponse.Message}");

        if (!kisResponse.HasData)
            throw new Exception("매수가능조회 데이터가 없습니다.");

        return _converter.ConvertToBuyableInquiryResponse(kisResponse.Output, request.OrderPrice, request.StockCode);
    }

    public async Task<CurrentPriceResponse> GetCurrentPriceAsync(CurrentPriceRequest request, UserInfo user)
    {
        var marketConstants = _settings.MarketConstants;
        var queryParams = new Dictionary<string, string>
        {
            ["FID_COND_MRKT_DIV_CODE"] = marketConstants.DomesticStock,
            ["FID_INPUT_ISCD"] = request.StockCode
        };

        var url = BuildGetUrl(_settings.Endpoints.CurrentPricePath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.DefaultValues.CurrentPriceTransactionId, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("KIS 현재가 조회 실패: StatusCode={StatusCode}, Content={Content}",
                response.StatusCode, responseContent);
            throw new Exception($"KIS 현재가 조회 실패 ({response.StatusCode}): {responseContent}");
        }

        var kisResponse = JsonSerializer.Deserialize<KisStockPriceResponse>(responseContent);

        if (!kisResponse.IsSuccess)
            throw new Exception($"현재가 조회 실패: {kisResponse.Message}");

        if (!kisResponse.HasData)
            throw new Exception("현재가 조회 데이터가 없습니다.");

        return _converter.ConvertToStockPriceResponse(kisResponse.Output, request.StockCode);
    }

    private static KisOrderRequest CreateKisOrderRequest(OrderRequest request, UserInfo user)
    {
        return new KisOrderRequest
        {
            CANO = user.AccountNumber,
            ACNT_PRDT_CD = "01",
            PDNO = request.PDNO,
            ORD_DVSN = request.ORD_DVSN,
            ORD_QTY = request.ORD_QTY,
            ORD_UNPR = request.ORD_UNPR,
        };
    }

    private HttpRequestMessage CreateOrderHttpRequest(KisOrderRequest kisRequest, string trId, UserInfo user)
    {
        var content = new StringContent(JsonSerializer.Serialize(kisRequest), Encoding.UTF8, "application/json");
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.OrderPath) { Content = content };
    
        SetStandardHeaders(httpRequest, trId, user);
        return httpRequest;
    }

    private async Task<OrderResponse> ProcessOrderResponse(HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("KIS 주문 응답: {Response}", responseContent);

        var orderResponse = JsonSerializer.Deserialize<OrderResponse>(responseContent);

        if (!orderResponse.IsSuccess)
            throw new Exception($"주문 실패: {orderResponse.Message}");

        if (!orderResponse.HasData)
            throw new Exception("주문 응답 데이터가 없습니다.");

        return orderResponse;
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

    private HttpRequestMessage CreateBalanceHttpRequest(Dictionary<string,string> queryParams, UserInfo user)
    {
        var url = BuildGetUrl(_settings.Endpoints.BalancePath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
    
        SetStandardHeaders(httpRequest, _settings.DefaultValues.BalanceTransactionId, user);
        return httpRequest;
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

    private Dictionary<string, string> CreateOrderExecutionQueryParams(OrderExecutionInquiryRequest request, UserInfo user)
    {
        var defaults = _settings.DefaultValues;
        return new Dictionary<string, string>
        {
            ["CANO"] = user.AccountNumber,
            ["ACNT_PRDT_CD"] = defaults.AccountProductCode,
            ["INQR_STRT_DT"] = request.StartDate,
            ["INQR_END_DT"] = request.EndDate,
            ["SLL_BUY_DVSN_CD"] = _converter.ConvertOrderTypeToKisCode(request.OrderType, _settings),
            ["INQR_DVSN"] = "00",
            ["PDNO"] = request.StockCode ?? "",
            ["CCLD_DVSN"] = "01",
            ["ORD_GNO_BRNO"] = "",
            ["ODNO"] = "",
            ["INQR_DVSN_3"] = "00",
            ["INQR_DVSN_1"] = "",
            ["CTX_AREA_FK100"] = "",
            ["CTX_AREA_NK100"] = ""
        };
    }

    private HttpRequestMessage CreateOrderExecutionHttpRequest(Dictionary<string, string> queryParams, UserInfo user)
    {
        var url = BuildGetUrl(_settings.Endpoints.OrderExecutionPath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
    
        SetOrderExecutionHeaders(httpRequest, _settings.DefaultValues.OrderExecutionTransactionId, user);
        return httpRequest;
    }

    private async Task<string> ValidateAndReadResponse(HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"KIS API 호출 실패 ({response.StatusCode}): {responseContent}");

        return responseContent;
    }

    private static void ValidateOrderExecutionResponse(KisOrderExecutionInquiryResponse kisResponse)
    {
        if (!kisResponse.IsSuccess)
            throw new Exception($"주문체결조회 실패: {kisResponse.Message}");

        if (!kisResponse.HasData)
            throw new Exception("주문체결조회 데이터가 없습니다.");
    }

    private void SetStandardHeaders(HttpRequestMessage httpRequestMessage, string trId, UserInfo user)
    {
        httpRequestMessage.Headers.Add("authorization", $"Bearer {user.KisToken.AccessToken}");
        httpRequestMessage.Headers.Add("appkey", user.KisAppKey);
        httpRequestMessage.Headers.Add("appsecret", user.KisAppSecret);
        httpRequestMessage.Headers.Add("tr_id", trId);
        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private void SetOrderExecutionHeaders(HttpRequestMessage httpRequestMessage, string trId, UserInfo user,
        string? trCont = null)
    {
        httpRequestMessage.Headers.Add("authorization", $"Bearer {user.KisToken.AccessToken}");
        httpRequestMessage.Headers.Add("appkey", user.KisAppKey);
        httpRequestMessage.Headers.Add("appsecret", user.KisAppSecret);
        httpRequestMessage.Headers.Add("tr_id", trId);
        httpRequestMessage.Headers.Add("custtype", "P"); // P: 개인, B: 법인

        // 연속조회 헤더 (필요시)
        if (!string.IsNullOrEmpty(trCont))
            httpRequestMessage.Headers.Add("tr_cont", trCont);

        // Accept 헤더
        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private static string BuildGetUrl(string basePath, Dictionary<string, string> queryParams)
    {
        var queryString = string.Join("&", queryParams
            .Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
        return $"{basePath}?{queryString}";
    }
}