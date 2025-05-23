using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Repositories;
using StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Models;
using StockTrading.Infrastructure.Implementations;
using static System.Text.Encoding;

namespace StockTrading.Tests.Integration.Services;

public class KisTokenServiceIntegrationTest
{
    private readonly Mock<IKisTokenRepository> _mockKisTokenRepository;
    private readonly Mock<IUserKisInfoRepository> _mockUserKisInfoRepository;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly ILogger<KisTokenService> _logger;
    private readonly KisTokenService _kisTokenService;
    private const string BASE_URL = "https://openapivts.koreainvestment.com:29443";

    public KisTokenServiceIntegrationTest()
    {
        _mockKisTokenRepository = new Mock<IKisTokenRepository>();
        _mockUserKisInfoRepository = new Mock<IUserKisInfoRepository>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _logger = new NullLogger<KisTokenService>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(BASE_URL)
        };

        _mockHttpClientFactory
            .Setup(f => f.CreateClient(nameof(KisTokenService)))
            .Returns(_httpClient);

        _kisTokenService = new KisTokenService(
            _mockHttpClientFactory.Object,
            _mockKisTokenRepository.Object,
            _mockUserKisInfoRepository.Object,
            _logger
        );
    }

    [Fact]
    public async Task GetKisTokenAsync_ValidCredentials_ReturnsTokenResponse()
    {
        int userId = 1;
        string appKey = "test_app_key";
        string appSecret = "test_app_secret";
        string accountNumber = "123456789";

        var tokenResponse = new TokenResponse
        {
            AccessToken = "test_access_token",
            TokenType = "Bearer",
            ExpiresIn = 86400
        };

        // HTTP 요청 모의 설정
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post && req.RequestUri.AbsolutePath.Contains("/oauth2/tokenP")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tokenResponse), UTF8, "application/json")
            });

        // 레포지토리 모의 설정
        _mockKisTokenRepository
            .Setup(repo => repo.SaveKisToken(userId, It.IsAny<TokenResponse>()))
            .Returns(Task.CompletedTask);
        _mockUserKisInfoRepository
            .Setup(repo => repo.UpdateUserKisInfo(userId, appKey, appSecret, accountNumber))
            .Returns(Task.CompletedTask);

        var result = await _kisTokenService.GetKisTokenAsync(userId, appKey, appSecret, accountNumber);

        Assert.NotNull(result);
        Assert.Equal(tokenResponse.AccessToken, result.AccessToken);
        Assert.Equal(tokenResponse.TokenType, result.TokenType);
        Assert.Equal(tokenResponse.ExpiresIn, result.ExpiresIn);

        _mockKisTokenRepository.Verify(
            repo => repo.SaveKisToken(userId, It.Is<TokenResponse>(t =>
                t.AccessToken == tokenResponse.AccessToken)),
            Times.Once);
        _mockUserKisInfoRepository.Verify(
            repo => repo.UpdateUserKisInfo(userId, appKey, appSecret, accountNumber),
            Times.Once);
    }

    [Fact]
    public async Task GetKisTokenAsync_InvalidCredentials_ThrowsHttpRequestException()
    {
        int userId = 1;
        string appKey = "invalid_app_key";
        string appSecret = "invalid_app_secret";
        string accountNumber = "123456789";

        // HTTP 요청 실패 모의 설정
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(
                    "{\"error\":\"invalid_client\",\"error_description\":\"Invalid client credentials\"}",
                    UTF8, "application/json"
                )
            });

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _kisTokenService.GetKisTokenAsync(userId, appKey, appSecret, accountNumber));

        _mockKisTokenRepository.Verify(
            repo => repo.SaveKisToken(It.IsAny<int>(), It.IsAny<TokenResponse>()),
            Times.Never);
        _mockUserKisInfoRepository.Verify(
            repo => repo.UpdateUserKisInfo(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetWebSocketTokenAsync_ValidCredentials_ReturnsApprovalKey()
    {
        int userId = 1;
        string appKey = "test_app_key";
        string appSecret = "test_app_secret";

        var webSocketResponse = new WebSocketApprovalResponse
        {
            ApprovalKey = "test_approval_key"
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.AbsolutePath.Contains("/oauth2/Approval")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(webSocketResponse), UTF8, "application/json")
            });

        _mockUserKisInfoRepository
            .Setup(repo => repo.SaveWebSocketTokenAsync(userId, webSocketResponse.ApprovalKey))
            .Returns(Task.CompletedTask);

        var result = await _kisTokenService.GetWebSocketTokenAsync(userId, appKey, appSecret);

        Assert.NotNull(result);
        Assert.Equal(webSocketResponse.ApprovalKey, result);

        _mockUserKisInfoRepository.Verify(
            repo => repo.SaveWebSocketTokenAsync(userId, webSocketResponse.ApprovalKey),
            Times.Once);
    }
}