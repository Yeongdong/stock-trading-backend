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

    #region Private Methods

    private KisOverseasOrderRequest CreateKisOverseasOrderRequest(OverseasOrderRequest request, UserInfo user)
    {
        var marketCode = GetMarketCode(request.Market);

        return new KisOverseasOrderRequest
        {
            CANO = user.AccountNumber,
            ACNT_PRDT_CD = _settings.DefaultValues.AccountProductCode,
            OVRS_EXCG_CD = request.OVRS_EXCG_CD,
            PDNO = request.PDNO,
            ORD_QTY = request.ORD_QTY,
            OVRS_ORD_UNPR = request.ORD_UNPR,
            ORD_SVR_DVSN_CD = request.ORD_DVSN,
            ORD_DVSN = request.ORD_CNDT
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

    #region Private Methods

    private Dictionary<string, string> CreateOverseasOrderExecutionQueryParams(string startDate, string endDate,
        UserInfo user)
    {
        return new Dictionary<string, string>
        {
            ["CANO"] = user.AccountNumber,
            ["ACNT_PRDT_CD"] = _settings.DefaultValues.AccountProductCode,
            ["OVRS_EXCG_CD"] = "NASD", // 기본값으로 나스닥, 필요시 파라미터로 받을 수 있음
            ["TR_CRCY_CD"] = "USD",
            ["ST_DT"] = startDate,
            ["END_DT"] = endDate,
            ["SLL_BUY_DVSN_CD"] = "00", // 전체 조회
            ["ORD_DVSN"] = "00" // 전체 조회
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

    #region Helper Methods

    private string GetMarketCode(StockTrading.Domain.Enums.Market market)
    {
        return market switch
        {
            Domain.Enums.Market.Nasdaq => "NASD",
            Domain.Enums.Market.Nyse => "NYSE",
            Domain.Enums.Market.Tokyo => "TKSE",
            Domain.Enums.Market.London => "LNSE",
            Domain.Enums.Market.Hongkong => "HKEX",
            _ => throw new ArgumentException($"지원하지 않는 해외 시장입니다: {market}")
        };
    }

    #endregion
}