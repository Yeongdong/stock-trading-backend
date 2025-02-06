using StockTradingBackend.DataAccess.Entities;

namespace stock_trading_backend.DTOs;

public class GoogleLoginResponse
{
    public UserDTO User { get; set; }
    public string Token { get; set; }

    public GoogleLoginResponse(User user, string token)
    {
        User = new UserDTO(user);
        Token = token;
    }
}