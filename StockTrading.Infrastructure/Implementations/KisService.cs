using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using stock_trading_backend.DTOs;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment;
using StockTrading.Infrastructure.Repositories;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Infrastructure.Implementations;

public class KisService : IKisService
{
    private readonly HttpClient _httpClient;
    private readonly KisApiClient _kisApiClient;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<KisService> _logger;
    private const string BASE_URL = "https://openapivts.koreainvestment.com:29443";

    public KisService(HttpClient httpClient, KisApiClient kisApiClient, ApplicationDbContext context,
        ILogger<KisService> logger)
    {
        _httpClient = httpClient;
        _kisApiClient = kisApiClient;
        _context = context;
        _logger = logger;
    }

    public async Task SaveTokenAsync(int userId, string accessToken, DateTime expiresIn, string tokenType)
    {
        try
        {
            _logger.LogInformation($"토큰 저장 시도 - UserId: {userId}");
            _logger.LogInformation($"토큰 저장 시도 - AccessToken: {accessToken}");

            var existingToken = await _context.KisTokens
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (existingToken != null)
            {
                _logger.LogInformation("기존 토큰 업데이트");
                existingToken.AccessToken = accessToken;
                existingToken.ExpiresIn = expiresIn;
                existingToken.TokenType = tokenType;
                _context.KisTokens.Update(existingToken);
            }
            else
            {
                _logger.LogInformation("새로운 토큰 생성");
                var newToken = new KisToken
                {
                    UserId = userId,
                    AccessToken = accessToken,
                    ExpiresIn = expiresIn,
                    TokenType = tokenType
                };
                await _context.KisTokens.AddAsync(newToken);
            }

            var result = await _context.SaveChangesAsync();
            _logger.LogInformation($"저장된 변경사항: {result}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"토큰 저장 중 에러 발생: {ex.Message}");
            _logger.LogError($"Stack trace: {ex.StackTrace}");
            throw;
        }
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

    public async Task<StockBalance> GetStockBalanceAsync(User user)
    {
        return await _kisApiClient.GetStockBalanceAsync(user);
    }
}