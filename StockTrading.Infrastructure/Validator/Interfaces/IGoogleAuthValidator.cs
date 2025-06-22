using Google.Apis.Auth;

namespace StockTrading.Infrastructure.Validator.Interfaces;

public interface IGoogleAuthValidator
{
    Task<GoogleJsonWebSignature.Payload> ValidateAsync(string token, string clientId);
}