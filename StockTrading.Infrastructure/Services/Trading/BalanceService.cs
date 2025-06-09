using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Trading.DTOs.Portfolio;
using StockTrading.Application.Features.Trading.Services;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common.Helpers;

namespace StockTrading.Infrastructure.Services.Trading;

public class BalanceService : IBalanceService
{
    private readonly IKisBalanceApiClient _kisBalanceApiClient; 

    public BalanceService(IKisBalanceApiClient kisBalanceApiClient)
    {
        _kisBalanceApiClient = kisBalanceApiClient;
    }
    
    public async Task<AccountBalance> GetStockBalanceAsync(UserInfo user)
    {
        KisValidationHelper.ValidateUserForKisApi(user);
        return await _kisBalanceApiClient.GetStockBalanceAsync(user);
    }
}