using System.Net;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using StockTrading.Application.DTOs.Common;
using StockTrading.Application.DTOs.External.KoreaInvestment;
using StockTrading.Application.Repositories;
using StockTrading.Infrastructure.Services;

namespace StockTrading.Tests.Unit.Implementations;

[TestSubject(typeof(KisTokenService))]
public class KisTokenServiceTest
{
    private readonly Mock<IKisTokenRepository> _mockUserTokenRepository;
    private readonly Mock<IUserKisInfoRepository> _mockUserKisInfoRepository;
    private readonly Mock<ILogger<KisTokenService>> _mockLogger;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly HttpClient _httpClient;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly KisTokenService _kisTokenService;
    private const string BASE_URL = "https://openapivts.koreainvestment.com:29443";

    public KisTokenServiceTest()
    {
        _mockUserTokenRepository = new Mock<IKisTokenRepository>();
        _mockUserKisInfoRepository = new Mock<IUserKisInfoRepository>();
        _mockLogger = new Mock<ILogger<KisTokenService>>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();

        _mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(BASE_URL)
        };
        _mockHttpClientFactory
            .Setup(f => f.CreateClient(nameof(KisTokenService)))
            .Returns(_httpClient);
        _kisTokenService = new KisTokenService(_mockHttpClientFactory.Object, _mockUserTokenRepository.Object,
            _mockUserKisInfoRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetKisTokenAsync_Success_ReturnsTokenResponse()
    {
        int userId = 1;
        string appKey = "test_app_key";
        string appSecret = "test_app_secret";
        string accountNumber = "12345678901234";

        var expectedTokenResponse = new TokenResponse
        {
            AccessToken = "test_access_token",
            TokenType = "Bearer",
            ExpiresIn = 86400,
        };

        var jsonResponse = JsonSerializer.Serialize(expectedTokenResponse);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.AbsolutePath.Contains("/oauth2/tokenP")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        _mockUserTokenRepository
            .Setup(repo => repo.SaveKisTokenAsync(userId, It.IsAny<TokenResponse>()))
            .Returns(Task.CompletedTask);

        _mockUserKisInfoRepository
            .Setup(repo => repo.UpdateUserKisInfoAsync(userId, appKey, appSecret, accountNumber))
            .Returns(Task.CompletedTask);

        var result = await _kisTokenService.GetKisTokenAsync(userId, appKey, appSecret, accountNumber);

        Assert.NotNull(result);
        Assert.Equal(expectedTokenResponse.AccessToken, result.AccessToken);
        Assert.Equal(expectedTokenResponse.TokenType, result.TokenType);
        Assert.Equal(expectedTokenResponse.ExpiresIn, result.ExpiresIn);
        _mockUserTokenRepository.Verify(
            repo => repo.SaveKisTokenAsync(userId, It.Is<TokenResponse>(t =>
                t.AccessToken == expectedTokenResponse.AccessToken)),
            Times.Once);
        _mockUserKisInfoRepository.Verify(
            repo => repo.UpdateUserKisInfoAsync(userId, appKey, appSecret, accountNumber),
            Times.Once);
    }

    [Fact]
    public async Task GetKisTokenAsync_HttpRequestException_ThrowsException()
    {
        int userId = 1;
        string appKey = "test_app_key";
        string appSecret = "test_app_secret";
        string accountNumber = "12345678901234";

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.AbsolutePath.Contains("/oauth2/tokenP")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Invalid request", Encoding.UTF8, "application/json")
            });

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _kisTokenService.GetKisTokenAsync(userId, appKey, appSecret, accountNumber));

        _mockUserTokenRepository.Verify(
            repo => repo.SaveKisTokenAsync(It.IsAny<int>(), It.IsAny<TokenResponse>()),
            Times.Never);
        _mockUserKisInfoRepository.Verify(
            repo => repo.UpdateUserKisInfoAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetKisTokenAsync_InvalidTokenResponse_ThrowsInvalidOperationException()
    {
        int userId = 1;
        string appKey = "test_app_key";
        string appSecret = "test_app_secret";
        string accountNumber = "12345678901234";

        var invalidTokenResponse = new TokenResponse
        {
            AccessToken = "",
            TokenType = "Bearer",
            ExpiresIn = 0,
        };

        var jsonResponse = JsonSerializer.Serialize(invalidTokenResponse);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _kisTokenService.GetKisTokenAsync(userId, appKey, appSecret, accountNumber));
    }

    [Fact]
    public async Task GetWebSocketTokenAsync_Success_ReturnsApprovalKey()
    {
        int userId = 1;
        string appKey = "test_app_key";
        string appSecret = "test_app_secret";

        var expectedResponse = new WebSocketApprovalResponse
        {
            ApprovalKey = "test_approval_key"
        };

        var jsonResponse = JsonSerializer.Serialize(expectedResponse);

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
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        _mockUserKisInfoRepository
            .Setup(repo => repo.SaveWebSocketTokenAsync(userId, expectedResponse.ApprovalKey))
            .Returns(Task.CompletedTask);

        var result = await _kisTokenService.GetWebSocketTokenAsync(userId, appKey, appSecret);

        Assert.Equal(expectedResponse.ApprovalKey, result);
        _mockUserKisInfoRepository.Verify(
            repo => repo.SaveWebSocketTokenAsync(userId, expectedResponse.ApprovalKey),
            Times.Once);
    }

    [Fact]
    public async Task GetWebSocketTokenAsync_HttpRequestException_ThrowsException()
    {
        int userId = 1;
        string appKey = "test_app_key";
        string appSecret = "test_app_secret";

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
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Invalid request", Encoding.UTF8, "application/json")
            });

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _kisTokenService.GetWebSocketTokenAsync(userId, appKey, appSecret));

        _mockUserKisInfoRepository.Verify(
            repo => repo.SaveWebSocketTokenAsync(It.IsAny<int>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetWebSocketTokenAsync_NullApprovalKey_ThrowsInvalidOperationException()
    {
        int userId = 1;
        string appKey = "test_app_key";
        string appSecret = "test_app_secret";

        var invalidResponse = new WebSocketApprovalResponse
        {
            ApprovalKey = null
        };

        var jsonResponse = JsonSerializer.Serialize(invalidResponse);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _kisTokenService.GetWebSocketTokenAsync(userId, appKey, appSecret));
    }
}