using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using StockTrading.Application.DTOs.Auth;
using StockTrading.Application.DTOs.External.KoreaInvestment;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Repositories;
using StockTrading.Application.Services;
using StockTrading.Infrastructure.Services.Helpers;

namespace StockTrading.Infrastructure.Services;

public class KisTokenService : IKisTokenService
{
    private readonly HttpClient _httpClient;
    private readonly IKisTokenRepository _kisTokenRepository;
    private readonly IUserKisInfoRepository _userKisInfoRepository;
    private readonly IDbContextWrapper _dbContextWrapper;
    private readonly ILogger<KisTokenService> _logger;


    public KisTokenService(IHttpClientFactory httpClientFactory, IKisTokenRepository kisTokenRepository,
        IUserKisInfoRepository userKisInfoRepository, IDbContextWrapper dbContextWrapper,
        ILogger<KisTokenService> logger)
    {
        _kisTokenRepository = kisTokenRepository;
        _userKisInfoRepository = userKisInfoRepository;
        _dbContextWrapper = dbContextWrapper;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(nameof(KisTokenService));
    }

    public async Task<TokenInfo> UpdateUserKisInfoAndTokensAsync(int userId, string appKey, string appSecret,
        string accountNumber)
    {
        KisValidationHelper.ValidateTokenRequest(userId, appKey, appSecret, accountNumber);

        _logger.LogInformation("KIS 정보 및 토큰 업데이트 시작: 사용자 {UserId}", userId);

        await using var transaction = await _dbContextWrapper.BeginTransactionAsync();

        var tokenResponse = await GetKisTokenAsync(userId, appKey, appSecret, accountNumber);
        await GetWebSocketTokenAsync(userId, appKey, appSecret);
        await transaction.CommitAsync();

        return tokenResponse;
    }

    public async Task<TokenInfo> GetKisTokenAsync(int userId, string appKey, string appSecret, string accountNumber)
    {
        var bodyData = new
        {
            grant_type = "client_credentials",
            appkey = appKey,
            appsecret = appSecret
        };

        var response = await _httpClient.PostAsJsonAsync("/oauth2/tokenP", bodyData);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenInfo>();
        _logger.LogInformation("KIS 토큰 발급 성공: {UserId}", userId);

        await _kisTokenRepository.SaveKisTokenAsync(userId, tokenResponse);
        await _userKisInfoRepository.UpdateUserKisInfoAsync(userId, appKey, appSecret, accountNumber);

        return tokenResponse;
    }

    public async Task<string> GetWebSocketTokenAsync(int userId, string appKey, string appSecret)
    {
        var content = new
        {
            grant_type = "client_credentials",
            appkey = appKey,
            secretkey = appSecret
        };

        var response = await _httpClient.PostAsJsonAsync("/oauth2/Approval", content);
        response.EnsureSuccessStatusCode();

        var WebTokenResponse = await response.Content.ReadFromJsonAsync<KisWebSocketApprovalResponse>();
        _logger.LogInformation("WebSocket 토큰 발급 성공: {WebSocketToken}", WebTokenResponse);

        await _userKisInfoRepository.SaveWebSocketTokenAsync(userId, WebTokenResponse.ApprovalKey);

        return WebTokenResponse.ApprovalKey;
    }
}