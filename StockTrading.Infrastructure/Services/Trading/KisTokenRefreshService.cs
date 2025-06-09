using StockTrading.Application.Features.Auth.DTOs;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Application.Features.Users.Services;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common.Helpers;

namespace StockTrading.Infrastructure.Services.Trading;

public class KisTokenRefreshService : IKisTokenRefreshService
{
    private readonly IKisTokenService _kisTokenService;

    public KisTokenRefreshService(IKisTokenService kisTokenService)
    {
        _kisTokenService = kisTokenService;
    }

    public async Task<bool> EnsureValidTokenAsync(UserInfo user)
    {
        if (!HasKisCredentials(user))
            return false;

        if (KisValidationHelper.IsTokenValid(user.KisToken))
            return false;

        return await RefreshTokenAsync(user);
    }

    private static bool HasKisCredentials(UserInfo user)
    {
        return !string.IsNullOrWhiteSpace(user.KisAppKey) &&
               !string.IsNullOrWhiteSpace(user.KisAppSecret) &&
               !string.IsNullOrWhiteSpace(user.AccountNumber);
    }

    private async Task<bool> RefreshTokenAsync(UserInfo user)
    {
        var newTokenInfo = await _kisTokenService.UpdateKisCredentialsAndTokensAsync(user.Id, user.KisAppKey, user.KisAppSecret, user.AccountNumber);
        UpdateUserTokenInfo(user, newTokenInfo);

        return true;
    }

    private static void UpdateUserTokenInfo(UserInfo user, TokenInfo newTokenInfo)
    {
        if (user.KisToken == null) 
            user.KisToken = new KisTokenInfo();

        user.KisToken.AccessToken = newTokenInfo.AccessToken;
        user.KisToken.TokenType = newTokenInfo.TokenType;
        user.KisToken.ExpiresIn = DateTime.UtcNow.AddSeconds(newTokenInfo.ExpiresIn);
    }
}