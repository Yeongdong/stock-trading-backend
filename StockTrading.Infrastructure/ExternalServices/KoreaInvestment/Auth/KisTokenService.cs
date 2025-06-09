using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using StockTrading.Application.Common.Interfaces;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.Features.Auth.DTOs;
using StockTrading.Application.Features.Users.Repositories;
using StockTrading.Application.Features.Users.Services;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common.Helpers;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Auth;

public class KisTokenService : IKisTokenService
{
    private readonly HttpClient _httpClient;
    private readonly ITokenRepository _tokenRepository;
    private readonly IUserKisInfoRepository _userKisInfoRepository;
    private readonly IDbContextWrapper _dbContextWrapper;
    private readonly ILogger<KisTokenService> _logger;


    public KisTokenService(IHttpClientFactory httpClientFactory, ITokenRepository tokenRepository,
        IUserKisInfoRepository userKisInfoRepository, IDbContextWrapper dbContextWrapper,
        ILogger<KisTokenService> logger)
    {
        _tokenRepository = tokenRepository;
        _userKisInfoRepository = userKisInfoRepository;
        _dbContextWrapper = dbContextWrapper;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(nameof(KisTokenService));
    }

    public async Task<TokenInfo> UpdateKisCredentialsAndTokensAsync(int userId, string appKey, string appSecret,
        string accountNumber)
    {
        KisValidationHelper.ValidateTokenRequest(userId, appKey, appSecret, accountNumber);

        _logger.LogInformation("KIS 정보 및 토큰 업데이트 시작: 사용자 {UserId}", userId);

        await using var transaction = await _dbContextWrapper.BeginTransactionAsync();

        var tokenResponse = await GetKisAccessTokenAsync(userId, appKey, appSecret, accountNumber);
        await GetKisWebSocketTokenAsync(userId, appKey, appSecret);
        await transaction.CommitAsync();

        return tokenResponse;
    }

    public async Task<TokenInfo> GetKisAccessTokenAsync(int userId, string appKey, string appSecret, string accountNumber)
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

        await _tokenRepository.SaveKisTokenAsync(userId, tokenResponse);
        await _userKisInfoRepository.UpdateKisCredentialsAsync(userId, appKey, appSecret, accountNumber);

        return tokenResponse;
    }

    public async Task<string> GetKisWebSocketTokenAsync(int userId, string appKey, string appSecret)
    {
        var content = new
        {
            grant_type = "client_credentials",
            appkey = appKey,
            secretkey = appSecret
        };

        var response = await _httpClient.PostAsJsonAsync("/oauth2/Approval", content);
        response.EnsureSuccessStatusCode();

        var webSocketTokenResponse = await response.Content.ReadFromJsonAsync<KisWebSocketApprovalResponse>();
        _logger.LogInformation("WebSocket 토큰 발급 성공: {WebSocketToken}", webSocketTokenResponse);

        await _userKisInfoRepository.SaveWebSocketTokenAsync(userId, webSocketTokenResponse.ApprovalKey);

        return webSocketTokenResponse.ApprovalKey;
    }
}