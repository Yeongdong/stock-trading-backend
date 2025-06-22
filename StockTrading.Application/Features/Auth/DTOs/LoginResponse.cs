using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Auth.DTOs;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public UserInfo User { get; init; } = new();
    public string? Message { get; set; }
    public bool IsAuthenticated { get; init; } = true;
}