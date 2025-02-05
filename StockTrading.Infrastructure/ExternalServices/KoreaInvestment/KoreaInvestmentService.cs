using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using stock_trading_backend;
using StockTrading.DataAccess.Services.Interfaces;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

public class KoreaInvestmentService: IKoreaInvestmentService
{
    private readonly HttpClient _httpClient;
    private const string BASE_URL = "https://openapivts.koreainvestment.com:29443";

    public KoreaInvestmentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TokenResponse> GetTokenAsync(string appKey, string appSecret)
    {
        var bodyData = new
        {
            grant_type = "client_credentials",
            appkey = appKey,
            appsecret = appSecret
        };

        var response = await _httpClient.PostAsJsonAsync($"{BASE_URL}/oauth2/tokenP", bodyData);

        var responseContent = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to get token: {responseContent}");
        }
        
        return await response.Content.ReadFromJsonAsync<TokenResponse>();
    }
}