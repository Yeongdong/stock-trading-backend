using System.Text.Json.Serialization;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Application.Features.Auth.DTOs;

public class LoginResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;
    
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; init; } = 3600;
    
    [JsonPropertyName("user")]
    public UserInfo User { get; init; } = new();
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("isAuthenticated")]
    public bool IsAuthenticated { get; init; } = true;
}