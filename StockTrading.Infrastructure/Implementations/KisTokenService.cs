using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;

namespace StockTrading.Infrastructure.Implementations;

public class KisTokenService: IKisTokenService
{
    
    private readonly HttpClient _httpClient;
    private readonly ILogger<KisTokenService> _logger;
    private const string BASE_URL = "https://openapivts.koreainvestment.com:29443";

    public KisTokenService(HttpClient httpClient, ILogger<KisTokenService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(BASE_URL);
    }

    public async Task<TokenResponse> GetKisTokenAsync(string appKey, string appSecret)
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
    
    public async Task<string> GetWebSocketTokenAsync(string appKey, string appSecret)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/oauth2/Approval");

            var content = new
            {
                grant_type = "client_credentials",
                appkey = appKey,
                appsecret = appSecret
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<WebSocketApprovalResponse>();
            return result.ApprovalKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get WebSocket approval key");
            throw;
        }
    }
}