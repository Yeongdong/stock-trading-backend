using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.External.KoreaInvestment.Requests;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Users.DTOs;
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

    #region 국내 주식

    public async Task<DomesticCurrentPriceResponse> GetDomesticCurrentPriceAsync(CurrentPriceRequest request, UserInfo user)
    {
        var queryParams = CreateCurrentPriceQueryParams(request);
        var httpRequest = CreateCurrentPriceHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var kisResponse = await response.Content.ReadFromJsonAsync<KisStockPriceResponse>();

        ValidateCurrentPriceResponse(kisResponse);

        return _priceConverter.ConvertToCurrentPriceResponse(kisResponse.Output, request.StockCode);
    }

    public async Task<PeriodPriceResponse> GetDomesticPeriodPriceAsync(PeriodPriceRequest request, UserInfo user)
    {
        var queryParams = CreatePeriodPriceQueryParams(request);
        var httpRequest = CreatePeriodPriceHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var kisResponse = await response.Content.ReadFromJsonAsync<KisPeriodPriceResponse>();

        ValidatePeriodPriceResponse(kisResponse);

        return _priceConverter.ConvertToPeriodPriceResponse(kisResponse, request.StockCode);
    }

    #endregion

    #region 해외 주식

    public async Task<OverseasCurrentPriceResponse> GetOverseasCurrentPriceAsync(OverseasPriceRequest request,
        UserInfo user)
    {
        var queryParams = CreateOverseasCurrentPriceQueryParams(request);
        var httpRequest = CreateOverseasCurrentPriceHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var kisResponse = await response.Content.ReadFromJsonAsync<KisOverseasPriceResponse>();

        ValidateOverseasPriceResponse(kisResponse);

        return _priceConverter.ConvertToOverseasCurrentPriceResponse(kisResponse.Output, request.StockCode);
    }
    
    public async Task<OverseasPeriodPriceResponse> GetOverseasPeriodPriceAsync(OverseasPeriodPriceRequest request, UserInfo user)
    {
        var queryParams = CreateOverseasPeriodPriceQueryParams(request);
        var httpRequest = CreateOverseasPeriodPriceHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var kisResponse = await response.Content.ReadFromJsonAsync<KisOverseasPeriodPriceResponse>();

        ValidateOverseasPeriodPriceResponse(kisResponse);

        return _priceConverter.ConvertToOverseasPeriodPriceResponse(kisResponse, request.StockCode);
    }

    #endregion

    #region 국내 주식 Private Methods

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
        var url = BuildGetUrl(_settings.Endpoints.DomesticCurrentPricePath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.DefaultValues.DomesticCurrentPriceTransactionId, user);
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
        var url = BuildGetUrl(_settings.Endpoints.DomesticPeriodPricePath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.DefaultValues.DomesticPeriodPriceTransactionId, user);
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

    #endregion

    #region 해외 주식 Private Methods

    private Dictionary<string, string> CreateOverseasCurrentPriceQueryParams(OverseasPriceRequest request)
    {
        return new Dictionary<string, string>
        {
            ["AUTH"] = "",
            ["EXCD"] = request.MarketCode,
            ["SYMB"] = request.StockCode
        };
    }

    private HttpRequestMessage CreateOverseasCurrentPriceHttpRequest(Dictionary<string, string> queryParams,
        UserInfo user)
    {
        var url = BuildGetUrl(_settings.Endpoints.OverseasCurrentPricePath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.DefaultValues.OverseasCurrentPriceTransactionId, user);
        return httpRequest;
    }

    private void ValidateOverseasPriceResponse(KisOverseasPriceResponse response)
    {
        if (!response.IsSuccess)
            throw new Exception($"해외 주식 현재가 조회 실패: {response.Message}");

        if (!response.HasData)
            throw new Exception("해외 주식 현재가 조회 데이터가 없습니다.");
    }

    private Dictionary<string, string> CreateOverseasPeriodPriceQueryParams(OverseasPeriodPriceRequest request)
    {
        return new Dictionary<string, string>
        {
            ["FID_COND_MRKT_DIV_CODE"] = request.MarketDivCode,
            ["FID_INPUT_ISCD"] = request.StockCode,
            ["FID_INPUT_DATE_1"] = request.StartDate,
            ["FID_INPUT_DATE_2"] = request.EndDate,
            ["FID_PERIOD_DIV_CODE"] = request.PeriodDivCode
        };
    }
    
    private HttpRequestMessage CreateOverseasPeriodPriceHttpRequest(Dictionary<string, string> queryParams, UserInfo user)
    {
        var url = BuildGetUrl(_settings.Endpoints.OverseasPeriodPricePath, queryParams);
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

        SetStandardHeaders(httpRequest, _settings.DefaultValues.OverseasPeriodPriceTransactionId, user);
        return httpRequest;
    }

    private void ValidateOverseasPeriodPriceResponse(KisOverseasPeriodPriceResponse? response)
    {
        if (response == null)
            throw new InvalidOperationException("해외 주식 기간별시세 조회 응답이 null입니다.");

        if (!response.IsSuccess)
            throw new InvalidOperationException($"해외 주식 기간별시세 조회 실패: {response.Message}");
    }
    
    #endregion
}