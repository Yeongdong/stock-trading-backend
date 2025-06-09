using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.External.KoreaInvestment.Requests;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Settings.ExternalServices;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Trading.Converters;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Trading;

public class KisOrderApiClient : KisApiClientBase, IKisOrderApiClient
{
    private readonly OrderDataConverter _orderConverter;

    public KisOrderApiClient(HttpClient httpClient, IOptions<KoreaInvestmentSettings> settings,
        OrderDataConverter orderConverter, ILogger<KisOrderApiClient> logger)
        : base(httpClient, settings, logger)
    {
        _orderConverter = orderConverter;
    }

    public async Task<OrderResponse> PlaceOrderAsync(OrderRequest request, UserInfo user)
    {
        var kisRequest = CreateKisOrderRequest(request, user);
        var httpRequest = CreateOrderHttpRequest(kisRequest, request.tr_id, user);

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();

        return await ProcessOrderResponse(response);
    }

    public async Task<OrderExecutionInquiryResponse> GetOrderExecutionsAsync(OrderExecutionInquiryRequest request,
        UserInfo user)
    {
        var queryParams = CreateOrderExecutionQueryParams(request, user);
        var httpRequest = CreateOrderExecutionHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var responseContent = await ValidateAndReadResponse(response);

        var kisResponse = JsonSerializer.Deserialize<KisOrderExecutionInquiryResponse>(responseContent);
        ValidateOrderExecutionResponse(kisResponse);

        return _orderConverter.ConvertToOrderExecutionResponse(kisResponse);
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

    private Dictionary<string, string> CreateOrderExecutionQueryParams(OrderExecutionInquiryRequest request,
        UserInfo user)
    {
        var defaults = _settings.DefaultValues;
        return new Dictionary<string, string>
        {
            ["CANO"] = user.AccountNumber,
            ["ACNT_PRDT_CD"] = defaults.AccountProductCode,
            ["INQR_STRT_DT"] = request.StartDate,
            ["INQR_END_DT"] = request.EndDate,
            ["SLL_BUY_DVSN_CD"] = _orderConverter.ConvertOrderTypeToKisCode(request.OrderType, _settings),
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

    private void SetOrderExecutionHeaders(HttpRequestMessage httpRequestMessage, string trId, UserInfo user,
        string? trCont = null)
    {
        httpRequestMessage.Headers.Add("authorization", $"Bearer {user.KisToken.AccessToken}");
        httpRequestMessage.Headers.Add("appkey", user.KisAppKey);
        httpRequestMessage.Headers.Add("appsecret", user.KisAppSecret);
        httpRequestMessage.Headers.Add("tr_id", trId);
        httpRequestMessage.Headers.Add("custtype", "P");

        // 연속조회 헤더 (필요시)
        if (!string.IsNullOrEmpty(trCont))
            httpRequestMessage.Headers.Add("tr_cont", trCont);

        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private static void ValidateOrderExecutionResponse(KisOrderExecutionInquiryResponse kisResponse)
    {
        if (!kisResponse.IsSuccess)
            throw new Exception($"주문체결조회 실패: {kisResponse.Message}");

        if (!kisResponse.HasData)
            throw new Exception("주문체결조회 데이터가 없습니다.");
    }
}