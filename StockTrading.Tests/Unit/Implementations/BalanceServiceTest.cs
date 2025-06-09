using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Trading.DTOs.Portfolio;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Infrastructure.Services;
using StockTrading.Infrastructure.Services.Trading;

namespace StockTrading.Tests.Unit.Implementations;

[TestSubject(typeof(BalanceService))]
public class BalanceServiceTest
{
    private readonly Mock<IKisBalanceApiClient> _mockKisApiClient;
    private readonly BalanceService _balanceService;

    public BalanceServiceTest()
    {
        _mockKisApiClient = new Mock<IKisBalanceApiClient>();

        _balanceService = new BalanceService(_mockKisApiClient.Object);
    }

    [Fact]
    public async Task GetStockBalanceAsync_ValidUser_ReturnsBalance()
    {
        // Arrange
        var userDto = CreateTestUser();
        var expectedBalance = CreateTestBalance();

        _mockKisApiClient
            .Setup(client => client.GetStockBalanceAsync(It.IsAny<UserInfo>()))
            .ReturnsAsync(expectedBalance);

        // Act
        var result = await _balanceService.GetStockBalanceAsync(userDto);

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
            _balanceService.GetStockBalanceAsync(null));
    }

    [Fact]
    public async Task GetStockBalanceAsync_UserWithoutKisAppKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var userDto = CreateTestUser();
        userDto.KisAppKey = null;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _balanceService.GetStockBalanceAsync(userDto));

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
            _balanceService.GetStockBalanceAsync(userDto));

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
            _balanceService.GetStockBalanceAsync(userDto));

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
            _balanceService.GetStockBalanceAsync(userDto));

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
            _balanceService.GetStockBalanceAsync(userDto));

        Assert.Equal("KIS 액세스 토큰이 만료되었습니다. 토큰을 재발급받아주세요.", exception.Message);
    }

    [Fact]
    public async Task GetStockBalanceAsync_ApiCallFails_ThrowsHttpRequestException()
    {
        // Arrange
        var userDto = CreateTestUser();

        _mockKisApiClient
            .Setup(client => client.GetStockBalanceAsync(It.IsAny<UserInfo>()))
            .ThrowsAsync(new HttpRequestException("네트워크 연결 오류"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _balanceService.GetStockBalanceAsync(userDto));
    }

    [Fact]
    public async Task GetStockBalanceAsync_EmptyPositions_ReturnsEmptyBalance()
    {
        // Arrange
        var userDto = CreateTestUser();
        var emptyBalance = new AccountBalance
        {
            Positions = [],
            Summary = new KisAccountSummaryResponse
            {
                TotalDeposit = "1000000",
                StockEvaluation = "0",
                TotalEvaluation = "1000000"
            }
        };

        _mockKisApiClient
            .Setup(client => client.GetStockBalanceAsync(It.IsAny<UserInfo>()))
            .ReturnsAsync(emptyBalance);

        // Act
        var result = await _balanceService.GetStockBalanceAsync(userDto);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Positions);
        Assert.Equal("0", result.Summary.StockEvaluation);
    }

    private static UserInfo CreateTestUser()
    {
        return new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            KisAppKey = "testAppKey",
            KisAppSecret = "testAppSecret",
            AccountNumber = "12345678901234",
            KisToken = new KisTokenInfo
            {
                Id = 1,
                AccessToken = "validToken",
                TokenType = "Bearer",
                ExpiresIn = DateTime.UtcNow.AddHours(1)
            }
        };
    }

    private static AccountBalance CreateTestBalance()
    {
        return new AccountBalance
        {
            Positions =
            [
                new KisPositionResponse()
                {
                    StockCode = "005930",
                    StockName = "삼성전자",
                    Quantity = "10",
                    AveragePrice = "65000",
                    CurrentPrice = "70000",
                    ProfitLoss = "50000",
                    ProfitLossRate = "7.69"
                }
            ],
            Summary = new KisAccountSummaryResponse
            {
                TotalDeposit = "1000000",
                StockEvaluation = "700000",
                TotalEvaluation = "1700000"
            }
        };
    }
}