using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Repositories;
using StockTrading.DataAccess.Services.Interfaces;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;

namespace StockTrading.Infrastructure.Implementations;

public class KisTokenService : IKisTokenService
{
    private readonly HttpClient _httpClient;
    private readonly IKisTokenRepository _kisTokenRepository;
    private readonly IUserKisInfoRepository _userKisInfoRepository;
    private readonly ILogger<KisTokenService> _logger;
    private const string BASE_URL = "https://openapivts.koreainvestment.com:29443";

    public KisTokenService(
        IHttpClientFactory httpClientFactory,
        IKisTokenRepository kisTokenRepository,
        IUserKisInfoRepository userKisInfoRepository,
        ILogger<KisTokenService> logger)
    {
        _kisTokenRepository = kisTokenRepository;
        _userKisInfoRepository = userKisInfoRepository;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(nameof(KisTokenService));
        _httpClient.BaseAddress = new Uri(BASE_URL);
    }

    public async Task<TokenResponse> GetKisTokenAsync(int userId, string appKey, string appSecret, string accountNumber)
    {
        try
        {
            var bodyData = new
            {
                grant_type = "client_credentials",
                appkey = appKey,
                appsecret = appSecret
            };

            var response = await _httpClient.PostAsJsonAsync("/oauth2/tokenP", bodyData);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Response Content: {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"토큰 발급 실패: {responseContent}");
                throw new HttpRequestException($"Failed to get token: {responseContent}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            _logger.LogInformation("토큰 발급 성공");

            await _kisTokenRepository.SaveKisToken(userId, tokenResponse);
            await _userKisInfoRepository.UpdateUserKisInfo(userId, appKey, appSecret, accountNumber);

            _logger.LogInformation("토큰 저장 성공");
            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError($"토큰 발급 중 에러 발생: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GetWebSocketTokenAsync(int userId, string appKey, string appSecret)
    {
        try
        {
            var content = new
            {
                grant_type = "client_credentials",
                appkey = appKey,
                secretkey = appSecret
            };

            var response = await _httpClient.PostAsJsonAsync("/oauth2/Approval", content);
            _logger.LogInformation(response.Content.ReadAsStringAsync().Result);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<WebSocketApprovalResponse>();
            await _userKisInfoRepository.SaveWebSocketTokenAsync(userId, result.ApprovalKey);
            _logger.LogInformation("웹토큰 저장 성공");

            return result.ApprovalKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get WebSocket approval key");
            throw;
        }
    }
}