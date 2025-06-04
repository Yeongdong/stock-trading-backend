using StockTrading.Application.DTOs.Trading.Portfolio;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Services;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Helpers;

namespace StockTrading.Infrastructure.Services;

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