using Google.Apis.Auth;
using StockTrading.Infrastructure.Validator.Interfaces;

namespace StockTrading.Infrastructure.Validator.Implementations;

public class GoogleAuthValidator : IGoogleAuthValidator
{
    public async Task<GoogleJsonWebSignature.Payload> ValidateAsync(string token, string clientId)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { clientId }
        };
        return await GoogleJsonWebSignature.ValidateAsync(token, settings);
    }
}