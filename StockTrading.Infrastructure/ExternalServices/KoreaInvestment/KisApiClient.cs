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
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Converters.StockDataConverter;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

/**
 * KIS와의 통신을 담당하는 클래스
 */
public class KisApiClient : IKisApiClient
{
    private readonly HttpClient _httpClient;
    private readonly KisApiSettings _settings;
    private readonly ILogger<KisApiClient> _logger;

    public KisApiClient(HttpClient httpClient, IOptions<KisApiSettings> settings, ILogger<KisApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<OrderResponse> PlaceOrderAsync(OrderRequest request, UserInfo user)
    {
        var kisRequest = new KisOrderRequest
        {
            CANO = user.AccountNumber,
            ACNT_PRDT_CD = "01",
            PDNO = request.PDNO,
            ORD_DVSN = request.ORD_DVSN,
            ORD_QTY = request.ORD_QTY,
            ORD_UNPR = request.ORD_UNPR,
        };

        var content = new StringContent(JsonSerializer.Serialize(kisRequest), Encoding.UTF8, "application/json");
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.OrderPath)
            { Content = content };
        SetStandardHeaders(httpRequest, request.tr_id, user);

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("KIS 주문 응답: {Response}", responseContent);

        var orderResponse = JsonSerializer.Deserialize<OrderResponse>(responseContent);

        if (!orderResponse.IsSuccess)
            throw new Exception($"주문 실패: {orderResponse.Message}");

        if (!orderResponse.HasData)
            throw new Exception("주문 응답 데이터가 없습니다.");

        return orderResponse;
    }

    public async Task<AccountBalance> GetStockBalanceAsync(UserInfo user)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["CANO"] = user.AccountNumber,
            ["ACNT_PRDT_CD"] = _settings.Defaults.AccountProductCode,
            ["AFHR_FLPR_YN"] = _settings.Defaults.AfterHoursForeignPrice,
            ["OFL_YN"] = _settings.Defaults.OfflineYn,
            ["INQR_DVSN"] = _settings.Defaults.InquiryDivision,
            ["UNPR_DVSN"] = _settings.Defaults.UnitPriceDivision,
            ["FUND_STTL_ICLD_YN"] = _settings.Defaults.FundSettlementInclude,
            ["FNCG_AMT_AUTO_RDPT_YN"] = _settings.Defaults.FinancingAmountAutoRedemption,
            ["PRCS_DVSN"] = _settings.Defaults.ProcessDivision,
            ["CTX_AREA_FK100"] = "",
            ["CTX_AREA_NK100"] = ""
        };

        var url = BuildGetUrl(_settings.Endpoints.BalancePath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.Defaults.BalanceTransactionId, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var kisResponse = await response.Content.ReadFromJsonAsync<KisBalanceResponse>();

        if (!kisResponse.IsSuccess)
            throw new Exception($"잔고조회 실패: {kisResponse.Message}");

        if (!kisResponse.HasData)
            throw new Exception("잔고조회 데이터가 없습니다.");

        return new AccountBalance
        {
            Positions = kisResponse.Positions,
            Summary = kisResponse.Summary.FirstOrDefault() ?? new KisAccountSummaryResponse()
        };
    }

    public async Task<OrderExecutionInquiryResponse> GetOrderExecutionsAsync(OrderExecutionInquiryRequest request,
        UserInfo user)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["CANO"] = user.AccountNumber,
            ["ACNT_PRDT_CD"] = _settings.Defaults.AccountProductCode,
            ["INQR_STRT_DT"] = request.StartDate,
            ["INQR_END_DT"] = request.EndDate,
            ["SLL_BUY_DVSN_CD"] = ConvertOrderTypeToKisCode(request.OrderType),
            ["INQR_DVSN"] = "00", // 조회구분
            ["PDNO"] = request.StockCode ?? "", // 종목코드
            ["CCLD_DVSN"] = "01", // 체결구분 (01:체결)
            ["ORD_GNO_BRNO"] = "", // 주문채번지점번호
            ["ODNO"] = "", // 주문번호
            ["INQR_DVSN_3"] = "00", // 조회구분3
            ["INQR_DVSN_1"] = "", // 조회구분1
            ["CTX_AREA_FK100"] = "", // 연속조회검색조건100
            ["CTX_AREA_NK100"] = "" // 연속조회키100
        };

        var url = BuildGetUrl(_settings.Endpoints.OrderExecutionPath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetOrderExecutionHeaders(httpRequest, _settings.Defaults.OrderExecutionTransactionId, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("KIS API 호출 실패: StatusCode={StatusCode}, Content={Content}",
                response.StatusCode, responseContent);
            throw new Exception($"KIS API 호출 실패 ({response.StatusCode}): {responseContent}");
        }

        var kisResponse = JsonSerializer.Deserialize<KisOrderExecutionInquiryResponse>(responseContent);

        if (!kisResponse.IsSuccess)
            throw new Exception($"주문체결조회 실패: {kisResponse.Message}");

        if (!kisResponse.HasData)
            throw new Exception("주문체결조회 데이터가 없습니다.");

        return ConvertToOrderExecutionResponse(kisResponse);
    }

    public async Task<BuyableInquiryResponse> GetBuyableInquiryAsync(BuyableInquiryRequest request, UserInfo user)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["CANO"] = user.AccountNumber,
            ["ACNT_PRDT_CD"] = _settings.Defaults.AccountProductCode,
            ["PDNO"] = request.StockCode,
            ["ORD_UNPR"] = request.OrderPrice.ToString("F0"),
            ["ORD_DVSN"] = request.OrderType,
            ["CMA_EVLU_AMT_ICLD_YN"] = "Y",
            ["OVRS_ICLD_YN"] = "N"
        };

        var url = BuildGetUrl(_settings.Endpoints.BuyableInquiryPath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.Defaults.BuyableInquiryTransactionId, user);

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();
        var kisResponse = await response.Content.ReadFromJsonAsync<KisBuyableInquiryResponse>();

        if (!kisResponse.IsSuccess)
            throw new Exception($"매수가능조회 실패: {kisResponse.Message}");

        if (!kisResponse.HasData)
            throw new Exception("매수가능조회 데이터가 없습니다.");

        return ConvertToBuyableInquiryResponse(kisResponse.Output, request.OrderPrice, request.StockCode);
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

    private string ConvertOrderTypeToKisCode(string orderType)
    {
        return orderType switch
        {
            "01" => _settings.Defaults.SellOrderCode, // 매도
            "02" => _settings.Defaults.BuyOrderCode, // 매수  
            _ => _settings.Defaults.AllOrderCode // 전체
        };
    }

    private static OrderExecutionInquiryResponse ConvertToOrderExecutionResponse(
        KisOrderExecutionInquiryResponse kisResponse)
    {
        var executionItems = kisResponse.ExecutionItems.Select(item => new OrderExecutionItem
        {
            OrderDate = item.OrderDate,
            OrderNumber = item.OrderNumber,
            StockCode = item.StockCode,
            StockName = item.StockName,
            OrderSide = item.SellBuyDivisionName,
            OrderQuantity = ParseIntSafely(item.OrderQuantity),
            OrderPrice = ParseDecimalSafely(item.OrderPrice),
            ExecutedQuantity = ParseIntSafely(item.TotalExecutedQuantity),
            ExecutedPrice = ParseDecimalSafely(item.AveragePrice),
            ExecutedAmount = ParseDecimalSafely(item.TotalExecutedAmount),
            OrderStatus = item.OrderStatusName,
            ExecutionTime = item.OrderTime
        }).ToList();

        return new OrderExecutionInquiryResponse
        {
            ExecutionItems = executionItems,
            TotalCount = executionItems.Count,
            HasMore = !string.IsNullOrEmpty(kisResponse.CtxAreaNk100)
        };
    }

    private static BuyableInquiryResponse ConvertToBuyableInquiryResponse(KisBuyableInquiryData kisData,
        decimal orderPrice, string stockCode)
    {
        var buyableAmount = ParseDecimalSafely(kisData.BuyableAmount);
        var calculatedQuantity = orderPrice > 0 ? (int)(buyableAmount / orderPrice) : 0;

        return new BuyableInquiryResponse
        {
            StockCode = stockCode,
            StockName = $"종목{stockCode}",
            BuyableAmount = buyableAmount,
            BuyableQuantity = Math.Max(calculatedQuantity, ParseIntSafely(kisData.BuyableQuantity)),
            OrderableAmount = buyableAmount,
            CashBalance = buyableAmount,
            OrderPrice = orderPrice,
            CurrentPrice = ParseDecimalSafely(kisData.CalculationPrice), 
            UnitQuantity = 1
        };
    }
}