namespace StockTrading.Application.Features.Auth.DTOs;

public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? Message { get; set; }
}