using StockTrading.Application.DTOs.External.KoreaInvestment.Requests;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Market.Services;
using StockTrading.Application.Features.Trading.DTOs.Inquiry;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Enums;
using static StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common.Helpers.KisValidationHelper;

namespace StockTrading.Infrastructure.Services.Trading;

public class CurrentPriceService : ICurrentPriceService
{
    private readonly IKisPriceApiClient _kisPriceApiClient;

    public CurrentPriceService(IKisPriceApiClient kisPriceApiClient)
    {
        _kisPriceApiClient = kisPriceApiClient;
    }

    #region 국내 주식

    public async Task<DomesticCurrentPriceResponse> GetDomesticCurrentPriceAsync(CurrentPriceRequest request, UserInfo userInfo)
    {
        ValidateUserForKisApi(userInfo);
        var response = await _kisPriceApiClient.GetDomesticCurrentPriceAsync(request, userInfo);
        return response;
    }

    #endregion

    #region 해외 주식

    public async Task<OverseasCurrentPriceResponse> GetOverseasCurrentPriceAsync(string stockCode, Domain.Enums.Market market, UserInfo userInfo)
    {
        ValidateUserForKisApi(userInfo);
        
        var request = new OverseasPriceRequest
        {
            StockCode = stockCode,
            MarketCode = GetMarketCode(market)
        };

        var response = await _kisPriceApiClient.GetOverseasCurrentPriceAsync(request, userInfo);
        return response;
    }

    #endregion

    #region Helper Methods

    private static string GetMarketCode(Domain.Enums.Market market)
    {
        return market switch
        {
            StockTrading.Domain.Enums.Market.Nasdaq => "NAS",
            StockTrading.Domain.Enums.Market.Nyse => "NYS",
            StockTrading.Domain.Enums.Market.Tokyo => "TSE",
            StockTrading.Domain.Enums.Market.London => "LSE",
            StockTrading.Domain.Enums.Market.HongKong => "HKSE",
            _ => throw new ArgumentException($"지원하지 않는 해외 시장: {market}")
        };
    }

    #endregion
}