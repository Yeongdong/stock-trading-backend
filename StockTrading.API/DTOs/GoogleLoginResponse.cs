using StockTrading.DataAccess.DTOs;

namespace stock_trading_backend.DTOs;

public class GoogleLoginResponse
{
    public UserDto User { get; set; }
    public string Token { get; set; }

    public GoogleLoginResponse(UserDto user, string token)
    {
        User = user;
        Token = token;
    }
}