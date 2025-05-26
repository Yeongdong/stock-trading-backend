using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.Common;

public class TokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; init; }

    [JsonPropertyName("access_token_token_expired")]
    public string AccessTokenTokenExpired { get; init; }

    [JsonPropertyName("token_type")] public string TokenType { get; init; }

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(AccessToken)
               && !string.IsNullOrEmpty(TokenType)
               && ExpiresIn > 0;
    }
}