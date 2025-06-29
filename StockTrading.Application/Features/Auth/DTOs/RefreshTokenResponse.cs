using System.Text.Json.Serialization;

namespace StockTrading.Application.Features.Auth.DTOs;

public class RefreshTokenResponse
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;
    
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; } = 3600;
    
    [JsonPropertyName("message")]
    public string? Message { get; init; }
}