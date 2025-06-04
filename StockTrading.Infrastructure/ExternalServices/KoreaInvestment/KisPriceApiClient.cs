using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.DTOs.Trading.Inquiry;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.ExternalServices;
using StockTrading.Domain.Settings;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Converters;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

public class KisPriceApiClient : KisApiClientBase, IKisPriceApiClient
{
    public KisPriceApiClient(HttpClient httpClient, IOptions<KoreaInvestmentSettings> settings,
        StockDataConverter converter, ILogger logger) : base(httpClient, settings, converter, logger)
    {
    }

    public async Task<KisCurrentPriceResponse> GetCurrentPriceAsync(CurrentPriceRequest request, UserInfo user)
    {
        var queryParams = CreateCurrentPriceQueryParams(request, user);
        var httpRequest = CreateCurrentPriceHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        var kisResponse = await response.Content.ReadFromJsonAsync<KisStockPriceResponse>();

        ValidateCurrentPriceResponse(kisResponse);

        return _converter.ConvertToStockPriceResponse(kisResponse.Output, request.StockCode);
    }

    private Dictionary<string, string> CreateCurrentPriceQueryParams(CurrentPriceRequest request, UserInfo user)
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
}