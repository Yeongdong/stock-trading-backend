using System.Text.Json.Serialization;

namespace StockTrading.API.DTOs.Requests;

public class UserInfoRequest 
{
    [JsonPropertyName("appKey")]
    public string AppKey { get; set; }
    
    [JsonPropertyName("appSecret")]
    public string AppSecret { get; set; }
    
    [JsonPropertyName("accountNumber")]
    public string AccountNumber { get; set; }
}