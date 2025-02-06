using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment;

public class KoreaInvestmentService : IKoreaInvestmentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KoreaInvestmentService> _logger;
    private const string BASE_URL = "https://openapivts.koreainvestment.com:29443";

    public KoreaInvestmentService(HttpClient httpClient, ILogger<KoreaInvestmentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TokenResponse> GetTokenAsync(string appKey, string appSecret)
    {
        try
        {
            var bodyData = new
            {
                grant_type = "client_credentials",
                appkey = appKey,
                appsecret = appSecret
            };

            var response = await _httpClient.PostAsJsonAsync($"{BASE_URL}/oauth2/tokenP", bodyData);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Response Status: {response.StatusCode}");
            _logger.LogInformation($"Response Content: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"토큰 발급 실패: {responseContent}");
                throw new HttpRequestException($"Failed to get token: {responseContent}");
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("Invalid token response");
            }

            _logger.LogInformation("토큰 발급 성공");
            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError($"토큰 발급 중 에러 발생: {ex.Message}");
            throw;
        }
    }
}