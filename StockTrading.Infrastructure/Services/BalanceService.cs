using StockTrading.Application.DTOs.Trading.Portfolio;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Services;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Helpers;

namespace StockTrading.Infrastructure.Services;

public class BalanceService : IBalanceService
{
    private readonly IKisApiClient _kisApiClient;

    public BalanceService(IKisApiClient kisApiClient)
    {
        _kisApiClient = kisApiClient;
    }
    
    public async Task<AccountBalance> GetStockBalanceAsync(UserInfo user)
    {
        KisValidationHelper.ValidateUserForKisApi(user);
        return await _kisApiClient.GetStockBalanceAsync(user);
    }
}