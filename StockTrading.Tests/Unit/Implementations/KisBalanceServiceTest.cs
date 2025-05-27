using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.Application.DTOs.Common;
using StockTrading.Application.DTOs.Stocks;
using StockTrading.Application.Services;
using StockTrading.Infrastructure.Services;

namespace StockTrading.Tests.Unit.Implementations;

[TestSubject(typeof(KisBalanceService))]
public class KisBalanceServiceTest
{
    private readonly Mock<IKisApiClient> _mockKisApiClient;
    private readonly Mock<ILogger<KisBalanceService>> _mockLogger;
    private readonly KisBalanceService _kisBalanceService;

    public KisBalanceServiceTest()
    {
        _mockKisApiClient = new Mock<IKisApiClient>();
        _mockLogger = new Mock<ILogger<KisBalanceService>>();

        _kisBalanceService = new KisBalanceService(
            _mockKisApiClient.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetStockBalanceAsync_ValidUser_ReturnsBalance()
    {
        // Arrange
        var userDto = CreateTestUser();
        var expectedBalance = CreateTestBalance();

        _mockKisApiClient
            .Setup(client => client.GetStockBalanceAsync(It.IsAny<UserDto>()))
            .ReturnsAsync(expectedBalance);

        // Act
        var result = await _kisBalanceService.GetStockBalanceAsync(userDto);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Positions);
        Assert.Equal("005930", result.Positions[0].StockCode);
        Assert.Equal("삼성전자", result.Positions[0].StockName);
        Assert.Equal("10", result.Positions[0].Quantity);
        Assert.Equal("1000000", result.Summary.TotalDeposit);
        
        _mockKisApiClient.Verify(client => client.GetStockBalanceAsync(userDto), Times.Once);
    }

    [Fact]
    public async Task GetStockBalanceAsync_NullUser_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _kisBalanceService.GetStockBalanceAsync(null));
    }

    [Fact]
    public async Task GetStockBalanceAsync_UserWithoutKisAppKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var userDto = CreateTestUser();
        userDto.KisAppKey = null;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _kisBalanceService.GetStockBalanceAsync(userDto));
        
        Assert.Equal("KIS 앱 키가 설정되지 않았습니다.", exception.Message);
    }

    [Fact]
    public async Task GetStockBalanceAsync_UserWithoutKisAppSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var userDto = CreateTestUser();
        userDto.KisAppSecret = "";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _kisBalanceService.GetStockBalanceAsync(userDto));
        
        Assert.Equal("KIS 앱 시크릿이 설정되지 않았습니다.", exception.Message);
    }

    [Fact]
    public async Task GetStockBalanceAsync_UserWithoutAccountNumber_ThrowsInvalidOperationException()
    {
        // Arrange
        var userDto = CreateTestUser();
        userDto.AccountNumber = "   "; // 공백

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _kisBalanceService.GetStockBalanceAsync(userDto));
        
        Assert.Equal("계좌번호가 설정되지 않았습니다.", exception.Message);
    }

    [Fact]
    public async Task GetStockBalanceAsync_UserWithoutKisToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var userDto = CreateTestUser();
        userDto.KisToken = null;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _kisBalanceService.GetStockBalanceAsync(userDto));
        
        Assert.Equal("KIS 액세스 토큰이 없습니다. 토큰을 먼저 발급받아주세요.", exception.Message);
    }

    [Fact]
    public async Task GetStockBalanceAsync_ExpiredToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var userDto = CreateTestUser();
        userDto.KisToken.ExpiresIn = DateTime.UtcNow.AddMinutes(-1); // 만료된 토큰

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _kisBalanceService.GetStockBalanceAsync(userDto));
        
        Assert.Equal("KIS 액세스 토큰이 만료되었습니다. 토큰을 재발급받아주세요.", exception.Message);
    }

    [Fact]
    public async Task GetStockBalanceAsync_ApiCallFails_ThrowsHttpRequestException()
    {
        // Arrange
        var userDto = CreateTestUser();

        _mockKisApiClient
            .Setup(client => client.GetStockBalanceAsync(It.IsAny<UserDto>()))
            .ThrowsAsync(new HttpRequestException("네트워크 연결 오류"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _kisBalanceService.GetStockBalanceAsync(userDto));
    }

    [Fact]
    public async Task GetStockBalanceAsync_EmptyPositions_ReturnsEmptyBalance()
    {
        // Arrange
        var userDto = CreateTestUser();
        var emptyBalance = new StockBalance
        {
            Positions = new List<Position>(),
            Summary = new Summary
            {
                TotalDeposit = "1000000",
                StockEvaluation = "0",
                TotalEvaluation = "1000000"
            }
        };

        _mockKisApiClient
            .Setup(client => client.GetStockBalanceAsync(It.IsAny<UserDto>()))
            .ReturnsAsync(emptyBalance);

        // Act
        var result = await _kisBalanceService.GetStockBalanceAsync(userDto);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Positions);
        Assert.Equal("0", result.Summary.StockEvaluation);
    }

    private static UserDto CreateTestUser()
    {
        return new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            KisAppKey = "testAppKey",
            KisAppSecret = "testAppSecret",
            AccountNumber = "12345678901234",
            KisToken = new KisTokenDto
            {
                Id = 1,
                AccessToken = "validToken",
                TokenType = "Bearer",
                ExpiresIn = DateTime.UtcNow.AddHours(1)
            }
        };
    }

    private static StockBalance CreateTestBalance()
    {
        return new StockBalance
        {
            Positions = new List<Position>
            {
                new Position
                {
                    StockCode = "005930",
                    StockName = "삼성전자",
                    Quantity = "10",
                    AveragePrice = "65000",
                    CurrentPrice = "70000",
                    ProfitLoss = "50000",
                    ProfitLossRate = "7.69"
                }
            },
            Summary = new Summary
            {
                TotalDeposit = "1000000",
                StockEvaluation = "700000",
                TotalEvaluation = "1700000"
            }
        };
    }
}