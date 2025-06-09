using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Settings;
using StockTrading.Domain.Settings.ExternalServices;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Market.Converters;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Market;

public class KisPriceApiClient : KisApiClientBase, IKisPriceApiClient
{
    private readonly PriceDataConverter _priceConverter;

    public KisPriceApiClient(HttpClient httpClient, IOptions<KoreaInvestmentSettings> settings,
        PriceDataConverter priceConverter, ILogger<KisPriceApiClient> logger) 
        : base(httpClient, settings, logger)
    {
        _priceConverter = priceConverter;
    }

    public async Task<KisCurrentPriceResponse> GetCurrentPriceAsync(CurrentPriceRequest request, UserInfo user)
    {
        var queryParams = CreateCurrentPriceQueryParams(request);
        var httpRequest = CreateCurrentPriceHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var kisResponse = await response.Content.ReadFromJsonAsync<KisStockPriceResponse>();

        ValidateCurrentPriceResponse(kisResponse);

        return _priceConverter.ConvertToCurrentPriceResponse(kisResponse.Output, request.StockCode);
    }

    public async Task<PeriodPriceResponse> GetPeriodPriceAsync(PeriodPriceRequest request, UserInfo user)
    {
        var queryParams = CreatePeriodPriceQueryParams(request);
        var httpRequest = CreatePeriodPriceHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var kisResponse = await response.Content.ReadFromJsonAsync<KisPeriodPriceResponse>();

        ValidatePeriodPriceResponse(kisResponse);

        return _priceConverter.ConvertToPeriodPriceResponse(kisResponse, request.StockCode);
    }

    private Dictionary<string, string> CreateCurrentPriceQueryParams(CurrentPriceRequest request)
    {
        var marketConstants = _settings.MarketConstants;
        return new Dictionary<string, string>
        {
            ["FID_COND_MRKT_DIV_CODE"] = marketConstants.DomesticStock,
            ["FID_INPUT_ISCD"] = request.StockCode
        };
    }

    private HttpRequestMessage CreateCurrentPriceHttpRequest(Dictionary<string, string> queryParams, UserInfo user)
    {
        var url = BuildGetUrl(_settings.Endpoints.CurrentPricePath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.DefaultValues.CurrentPriceTransactionId, user);
        return httpRequest;
    }

    private void ValidateCurrentPriceResponse(KisStockPriceResponse response)
    {
        if (!response.IsSuccess)
            throw new Exception($"현재가 조회 실패: {response.Message}");

        if (!response.HasData)
            throw new Exception("현재가 조회 데이터가 없습니다.");
    }

    private Dictionary<string, string> CreatePeriodPriceQueryParams(PeriodPriceRequest request)
    {
        return new Dictionary<string, string>
        {
            ["FID_COND_MRKT_DIV_CODE"] = request.MarketDivCode,
            ["FID_INPUT_ISCD"] = request.StockCode,
            ["FID_INPUT_DATE_1"] = request.StartDate,
            ["FID_INPUT_DATE_2"] = request.EndDate,
            ["FID_PERIOD_DIV_CODE"] = request.PeriodDivCode,
            ["FID_ORG_ADJ_PRC"] = request.OrgAdjPrc
        };
    }

    private HttpRequestMessage CreatePeriodPriceHttpRequest(Dictionary<string, string> queryParams, UserInfo user)
    {
        var url = BuildGetUrl(_settings.Endpoints.PeriodPricePath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.DefaultValues.PeriodPriceTransactionId, user);
        return httpRequest;
    }

    private void ValidatePeriodPriceResponse(KisPeriodPriceResponse response)
    {
        if (!response.IsSuccess)
        {
            var errorMessage = !string.IsNullOrEmpty(response.Message)
                ? response.Message
                : "기간별 시세 조회 API 호출 실패";
            throw new Exception($"API 오류: {errorMessage} (코드: {response.ReturnCode})");
        }

        if (!response.HasData || response.CurrentInfo == null)
            throw new Exception("기간별 시세 조회 응답 데이터가 없습니다.");

        if (!response.HasPriceData)
            throw new Exception("기간별 시세 조회 데이터가 없습니다.");
    }
}