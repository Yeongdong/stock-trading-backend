using Google.Apis.Auth;

namespace stock_trading_backend.Validator.Interfaces;

public interface IGoogleAuthValidator
{
    Task<GoogleJsonWebSignature.Payload> ValidateAsync(string token, string clientId);
}