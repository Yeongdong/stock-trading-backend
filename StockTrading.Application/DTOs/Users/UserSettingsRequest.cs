using System.Text.Json.Serialization;

namespace StockTrading.Application.DTOs.Users;

public class UserSettingsRequest 
{
    [JsonPropertyName("appKey")]
    public string AppKey { get; set; }
    
    [JsonPropertyName("appSecret")]
    public string AppSecret { get; set; }
    
    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; }
}