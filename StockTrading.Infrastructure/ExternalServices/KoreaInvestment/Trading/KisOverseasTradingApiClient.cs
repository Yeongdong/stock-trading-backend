using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.External.KoreaInvestment.Requests;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Trading.DTOs.Portfolio;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Settings.ExternalServices;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Trading.Converters;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Trading;

public class KisOverseasTradingApiClient : KisApiClientBase, IKisOverseasTradingApiClient
{
    private readonly OverseasOrderDataConverter _orderConverter;

    public KisOverseasTradingApiClient(HttpClient httpClient, IOptions<KoreaInvestmentSettings> settings,
        OverseasOrderDataConverter orderConverter, ILogger<KisOverseasTradingApiClient> logger)
        : base(httpClient, settings, logger)
    {
        _orderConverter = orderConverter;
    }

    #region 해외 주식 주문

    public async Task<OverseasOrderResponse> PlaceOverseasOrderAsync(OverseasOrderRequest request, UserInfo user)
    {
        var kisRequest = CreateKisOverseasOrderRequest(request, user);
        var httpRequest = CreateOverseasOrderHttpRequest(kisRequest, request.tr_id, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var responseContent = await ValidateAndReadResponse(response);

        var kisResponse = JsonSerializer.Deserialize<KisOverseasOrderResponse>(responseContent);
        ValidateOverseasOrderResponse(kisResponse);

        return _orderConverter.ConvertToOverseasOrderResponse(kisResponse, request);
    }

    #endregion

    #region 해외 주식 체결 내역 조회

    public async Task<List<OverseasOrderExecution>> GetOverseasOrderExecutionsAsync(string startDate, string endDate,
        UserInfo user)
    {
        var queryParams = CreateOverseasOrderExecutionQueryParams(startDate, endDate, user);
        var httpRequest = CreateOverseasOrderExecutionHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var responseContent = await ValidateAndReadResponse(response);

        var kisResponse = JsonSerializer.Deserialize<KisOverseasOrderExecutionResponse>(responseContent);
        ValidateOverseasOrderExecutionResponse(kisResponse);

        return _orderConverter.ConvertToOverseasOrderExecutions(kisResponse);
    }

    #endregion

    #region 주문 Private Methods

    private KisOverseasOrderRequest CreateKisOverseasOrderRequest(OverseasOrderRequest request, UserInfo user)
    {
        return new KisOverseasOrderRequest
        {
            CANO = user.AccountNumber,
            ACNT_PRDT_CD = request.ACNT_PRDT_CD,
            OVRS_EXCG_CD = request.OVRS_EXCG_CD,
            PDNO = request.PDNO,
            ORD_QTY = request.ORD_QTY,
            OVRS_ORD_UNPR = request.OVRS_ORD_UNPR,
            ORD_SVR_DVSN_CD = request.ORD_SVR_DVSN_CD,
            ORD_DVSN = request.ORD_DVSN
        };
    }

    private HttpRequestMessage CreateOverseasOrderHttpRequest(KisOverseasOrderRequest kisRequest, string tradeType,
        UserInfo user)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.OverseasOrderPath);

        var jsonContent = JsonSerializer.Serialize(kisRequest);
        httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        SetStandardHeaders(httpRequest, tradeType, user);
        return httpRequest;
    }

    private void ValidateOverseasOrderResponse(KisOverseasOrderResponse? response)
    {
        if (!response?.IsSuccess ?? true)
            throw new Exception($"해외 주식 주문 실패: {response?.Message ?? "응답 없음"}");

        if (!response.HasData)
            throw new Exception("해외 주식 주문 응답 데이터가 없습니다.");
    }

    #endregion

    #region 체결내역 Private Methods

    private Dictionary<string, string> CreateOverseasOrderExecutionQueryParams(string startDate, string endDate,
        UserInfo user)
    {
        return new Dictionary<string, string>
        {
            ["CANO"] = user.AccountNumber,
            ["ACNT_PRDT_CD"] = _settings.DefaultValues.AccountProductCode,
            ["PDNO"] = "%",                    // 전종목 조회
            ["ORD_STRT_DT"] = startDate,       // 주문시작일자 (YYYYMMDD)
            ["ORD_END_DT"] = endDate,          // 주문종료일자 (YYYYMMDD)
            ["SLL_BUY_DVSN"] = "00",          // 매도매수구분 (00:전체, 01:매도, 02:매수)
            ["CCLD_NCCS_DVSN"] = "01",        // 체결미체결구분 (00:전체, 01:체결, 02:미체결)
            ["OVRS_EXCG_CD"] = "%",           // 해외거래소코드 (전체 조회)
            ["SORT_SQN"] = "DS",              // 정렬순서 (DS:정순, AS:역순)
            ["ORD_DT"] = "",                  // 주문일자 (Null 값)
            ["ORD_GNO_BRNO"] = "",            // 주문채번지점번호 (Null 값)
            ["ODNO"] = "",                    // 주문번호 (Null 값)
            ["CTX_AREA_NK200"] = "",          // 연속조회키200 (첫 조회시 공백)
            ["CTX_AREA_FK200"] = ""           // 연속조회검색조건200 (첫 조회시 공백)
        };
    }

    private HttpRequestMessage CreateOverseasOrderExecutionHttpRequest(Dictionary<string, string> queryParams,
        UserInfo user)
    {
        var url = BuildGetUrl(_settings.Endpoints.OverseasOrderExecutionPath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.DefaultValues.OverseasOrderExecutionTransactionId, user);
        return httpRequest;
    }

    private void ValidateOverseasOrderExecutionResponse(KisOverseasOrderExecutionResponse? response)
    {
        if (!response?.IsSuccess ?? true)
            throw new Exception($"해외 주식 체결 내역 조회 실패: {response?.Message ?? "응답 없음"}");
    }

    #endregion
}