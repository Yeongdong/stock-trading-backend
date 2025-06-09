using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Auth.DTOs;

public class LoginResponse
{
    public UserInfo User { get; set; }
    public bool IsAuthenticated { get; set; }
    public string Message { get; set; }
}