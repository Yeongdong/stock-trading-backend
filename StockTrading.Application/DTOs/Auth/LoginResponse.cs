using StockTrading.Application.DTOs.Users;

namespace StockTrading.Application.DTOs.Auth;

public class LoginResponse
{
    public UserInfo User { get; set; }
    public bool IsAuthenticated { get; set; }
    public string Message { get; set; }
}