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

    #region 해외 주식 예약 주문

    public async Task<OverseasOrderResponse> PlaceScheduledOverseasOrderAsync(ScheduledOverseasOrderRequest request,
        UserInfo user)
    {
        var tradeId = GetScheduledOrderTradeId(request);
        var kisRequest = CreateKisScheduledOverseasOrderRequest(request, user);
        var httpRequest = CreateScheduledOrderHttpRequest(kisRequest, tradeId, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var responseContent = await ValidateAndReadResponse(response);

        var kisResponse = JsonSerializer.Deserialize<KisScheduledOverseasOrderResponse>(responseContent);
        ValidateScheduledOrderResponse(kisResponse);

        return _orderConverter.ConvertToScheduledOrderResponse(kisResponse, request);
    }

    #endregion

    #region 해외 주식 체결 내역 조회

    public async Task<KisOverseasOrderExecutionResponse> GetOverseasOrderExecutionsAsync(string startDate,
        string endDate,
        UserInfo user)
    {
        var queryParams = CreateOverseasOrderExecutionQueryParams(startDate, endDate, user);
        var httpRequest = CreateOverseasOrderExecutionHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var responseContent = await ValidateAndReadResponse(response);

        var kisResponse = JsonSerializer.Deserialize<KisOverseasOrderExecutionResponse>(responseContent);
        ValidateOverseasOrderExecutionResponse(kisResponse);

        var result = _orderConverter.ConvertToOverseasOrderExecutions(kisResponse);

        return kisResponse ?? new KisOverseasOrderExecutionResponse();
    }

    #endregion

    #region 즉시 주문 Private Methods

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

    #region 예약주문 Private Methods

    private KisScheduledOverseasOrderRequest CreateKisScheduledOverseasOrderRequest(
        ScheduledOverseasOrderRequest request, UserInfo user)
    {
        var isUsOrder = IsUsOrder(request.OVRS_EXCG_CD);
        var isBuyOrder = request.tr_id.Contains("1002") || request.tr_id.Contains("3014");

        var kisRequest = new KisScheduledOverseasOrderRequest
        {
            CANO = user.AccountNumber,
            ACNT_PRDT_CD = request.ACNT_PRDT_CD,
            PDNO = request.PDNO,
            OVRS_EXCG_CD = request.OVRS_EXCG_CD,
            FT_ORD_QTY = request.ORD_QTY,
            FT_ORD_UNPR3 = request.OVRS_ORD_UNPR,
            ORD_SVR_DVSN_CD = "0"
        };

        // 미국 주문과 아시아 주문 구분해서 필드 설정
        if (isUsOrder)
        {
            kisRequest.ORD_DVSN = "00"; // 지정가
            kisRequest.ALGO_ORD_TMD_DVSN_CD = "02"; // 예약주문은 시간입력 불가하여 02로 고정
        }
        else
        {
            kisRequest.SLL_BUY_DVSN_CD = isBuyOrder ? "02" : "01"; // 매수:02, 매도:01
            kisRequest.RVSE_CNCL_DVSN_CD = "00"; // 매도/매수 주문
            kisRequest.PRDT_TYPE_CD = GetProductTypeCode(request.OVRS_EXCG_CD);
            kisRequest.RSVN_ORD_RCIT_DT = DateTime.Today.ToString("yyyyMMdd");
        }

        return kisRequest;
    }

    private HttpRequestMessage CreateScheduledOrderHttpRequest(KisScheduledOverseasOrderRequest kisRequest,
        string tradeId, UserInfo user)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.Endpoints.OverseasScheduledOrderPath);

        var jsonContent = JsonSerializer.Serialize(kisRequest);
        httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        SetStandardHeaders(httpRequest, tradeId, user);
        return httpRequest;
    }

    private void ValidateScheduledOrderResponse(KisScheduledOverseasOrderResponse? response)
    {
        if (!response?.IsSuccess ?? true)
            throw new Exception($"해외 주식 예약주문 실패: {response?.Message ?? "응답 없음"}");

        if (!response.HasData)
            throw new Exception("해외 주식 예약주문 응답 데이터가 없습니다.");
    }

    private string GetScheduledOrderTradeId(ScheduledOverseasOrderRequest request)
    {
        var isUsOrder = IsUsOrder(request.OVRS_EXCG_CD);
        var isBuyOrder = request.tr_id.Contains("1002") || request.tr_id.Contains("3014");

        if (isUsOrder)
            return isBuyOrder ? "VTTT3014U" : "VTTT3016U"; // 미국 예약 매수/매도
        return "VTTS3013U"; // 아시아 예약주문 (통합)
    }

    private bool IsUsOrder(string exchangeCode)
    {
        return exchangeCode == "NASD" || exchangeCode == "NYSE" || exchangeCode == "AMEX";
    }

    private string GetProductTypeCode(string exchangeCode)
    {
        return exchangeCode switch
        {
            "TKSE" => "515", // 일본
            "SEHK" => "501", // 홍콩
            "SHAA" => "551", // 중국 상해A
            "SZAA" => "552", // 중국 심천A
            "HASE" => "507", // 베트남 하노이
            "VNSE" => "508", // 베트남 호치민
            _ => "501" // 기본값
        };
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
            ["PDNO"] = "%", // 전종목 조회
            ["ORD_STRT_DT"] = startDate, // 주문시작일자 (YYYYMMDD)
            ["ORD_END_DT"] = endDate, // 주문종료일자 (YYYYMMDD)
            ["SLL_BUY_DVSN"] = "00", // 매도매수구분 (00:전체, 01:매도, 02:매수)
            ["CCLD_NCCS_DVSN"] = "01", // 체결미체결구분 (00:전체, 01:체결, 02:미체결)
            ["OVRS_EXCG_CD"] = "%", // 해외거래소코드 (전체 조회)
            ["SORT_SQN"] = "DS", // 정렬순서 (DS:정순, AS:역순)
            ["ORD_DT"] = "", // 주문일자 (Null 값)
            ["ORD_GNO_BRNO"] = "", // 주문채번지점번호 (Null 값)
            ["ODNO"] = "", // 주문번호 (Null 값)
            ["CTX_AREA_NK200"] = "", // 연속조회키200 (첫 조회시 공백)
            ["CTX_AREA_FK200"] = "" // 연속조회검색조건200 (첫 조회시 공백)
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