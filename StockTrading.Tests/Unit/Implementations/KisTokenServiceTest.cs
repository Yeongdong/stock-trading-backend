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
    private readonly Mock<IKisTokenRepository> _mockKisTokenRepository;
    private readonly Mock<IUserKisInfoRepository> _mockUserKisInfoRepository;
    private readonly Mock<IDbContextWrapper> _mockDbContextWrapper;
    private readonly Mock<IDbTransactionWrapper> _mockDbTransaction;
    private readonly Mock<ILogger<KisTokenService>> _mockLogger;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly HttpClient _httpClient;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly KisTokenService _kisTokenService;
    private const string BASE_URL = "https://openapivts.koreainvestment.com:29443";

    public KisTokenServiceTest()
    {
        _mockKisTokenRepository = new Mock<IKisTokenRepository>();
        _mockUserKisInfoRepository = new Mock<IUserKisInfoRepository>();
        _mockDbContextWrapper = new Mock<IDbContextWrapper>();
        _mockDbTransaction = new Mock<IDbTransactionWrapper>();
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

        _mockDbContextWrapper
            .Setup(db => db.BeginTransactionAsync())
            .ReturnsAsync(_mockDbTransaction.Object);

        _kisTokenService = new KisTokenService(
            _mockHttpClientFactory.Object,
            _mockKisTokenRepository.Object,
            _mockUserKisInfoRepository.Object,
            _mockDbContextWrapper.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task UpdateUserKisInfoAndTokensAsync_Success_ReturnsTokenResponse()
    {
        // Arrange
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

        var expectedWebSocketResponse = new WebSocketApprovalResponse
        {
            ApprovalKey = "test_approval_key"
        };

        SetupHttpResponse("/oauth2/tokenP", expectedTokenResponse);
        SetupHttpResponse("/oauth2/Approval", expectedWebSocketResponse);

        _mockKisTokenRepository
            .Setup(repo => repo.SaveKisTokenAsync(userId, It.IsAny<TokenResponse>()))
            .Returns(Task.CompletedTask);

        _mockUserKisInfoRepository
            .Setup(repo => repo.UpdateUserKisInfoAsync(userId, appKey, appSecret, accountNumber))
            .Returns(Task.CompletedTask);

        _mockUserKisInfoRepository
            .Setup(repo => repo.SaveWebSocketTokenAsync(userId, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _kisTokenService.UpdateUserKisInfoAndTokensAsync(userId, appKey, appSecret, accountNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTokenResponse.AccessToken, result.AccessToken);
        Assert.Equal(expectedTokenResponse.TokenType, result.TokenType);
        Assert.Equal(expectedTokenResponse.ExpiresIn, result.ExpiresIn);

        _mockDbTransaction.Verify(t => t.CommitAsync(), Times.Once);
        _mockKisTokenRepository.Verify(repo => repo.SaveKisTokenAsync(userId, It.IsAny<TokenResponse>()), Times.Once);
        _mockUserKisInfoRepository.Verify(repo => repo.UpdateUserKisInfoAsync(userId, appKey, appSecret, accountNumber),
            Times.Once);
        _mockUserKisInfoRepository.Verify(
            repo => repo.SaveWebSocketTokenAsync(userId, expectedWebSocketResponse.ApprovalKey), Times.Once);
    }

    [Fact]
    public async Task UpdateUserKisInfoAndTokensAsync_InvalidUserId_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _kisTokenService.UpdateUserKisInfoAndTokensAsync(0, "key", "secret", "account"));

        Assert.Equal("유효하지 않은 사용자 ID입니다. (Parameter 'userId')", exception.Message);
    }

    [Fact]
    public async Task UpdateUserKisInfoAndTokensAsync_EmptyAppKey_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _kisTokenService.UpdateUserKisInfoAndTokensAsync(1, "", "secret", "account"));

        Assert.Equal("앱 키는 필수입니다. (Parameter 'appKey')", exception.Message);
    }

    [Fact]
    public async Task GetKisTokenAsync_Success_ReturnsTokenResponse()
    {
        // Arrange
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

        SetupHttpResponse("/oauth2/tokenP", expectedTokenResponse);

        _mockKisTokenRepository
            .Setup(repo => repo.SaveKisTokenAsync(userId, It.IsAny<TokenResponse>()))
            .Returns(Task.CompletedTask);

        _mockUserKisInfoRepository
            .Setup(repo => repo.UpdateUserKisInfoAsync(userId, appKey, appSecret, accountNumber))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _kisTokenService.GetKisTokenAsync(userId, appKey, appSecret, accountNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTokenResponse.AccessToken, result.AccessToken);
        Assert.Equal(expectedTokenResponse.TokenType, result.TokenType);
        Assert.Equal(expectedTokenResponse.ExpiresIn, result.ExpiresIn);
    }

    [Fact]
    public async Task GetKisTokenAsync_HttpRequestException_ThrowsException()
    {
        // Arrange
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

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _kisTokenService.GetKisTokenAsync(userId, appKey, appSecret, accountNumber));

        _mockKisTokenRepository.Verify(
            repo => repo.SaveKisTokenAsync(It.IsAny<int>(), It.IsAny<TokenResponse>()),
            Times.Never);
    }

    [Fact]
    public async Task GetWebSocketTokenAsync_Success_ReturnsApprovalKey()
    {
        // Arrange
        int userId = 1;
        string appKey = "test_app_key";
        string appSecret = "test_app_secret";

        var expectedResponse = new WebSocketApprovalResponse
        {
            ApprovalKey = "test_approval_key"
        };

        SetupHttpResponse("/oauth2/Approval", expectedResponse);

        _mockUserKisInfoRepository
            .Setup(repo => repo.SaveWebSocketTokenAsync(userId, expectedResponse.ApprovalKey))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _kisTokenService.GetWebSocketTokenAsync(userId, appKey, appSecret);

        // Assert
        Assert.Equal(expectedResponse.ApprovalKey, result);
        _mockUserKisInfoRepository.Verify(
            repo => repo.SaveWebSocketTokenAsync(userId, expectedResponse.ApprovalKey),
            Times.Once);
    }

    [Fact]
    public async Task GetWebSocketTokenAsync_NullApprovalKey_ThrowsInvalidOperationException()
    {
        // Arrange
        int userId = 1;
        string appKey = "test_app_key";
        string appSecret = "test_app_secret";

        var invalidResponse = new WebSocketApprovalResponse
        {
            ApprovalKey = null
        };

        SetupHttpResponse("/oauth2/Approval", invalidResponse);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _kisTokenService.GetWebSocketTokenAsync(userId, appKey, appSecret));
    }

    private void SetupHttpResponse<T>(string path, T responseObject)
    {
        var jsonResponse = JsonSerializer.Serialize(responseObject);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.AbsolutePath.Contains(path)),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });
    }
}