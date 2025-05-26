using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.Application.DTOs.Common;
using StockTrading.Application.DTOs.Orders;
using StockTrading.Application.DTOs.Stocks;
using StockTrading.Application.Repositories;
using StockTrading.Application.Services;
using StockTrading.Domain.Entities;
using StockTrading.Infrastructure.Services;
using static System.Net.HttpStatusCode;

namespace StockTrading.Tests.Unit.Implementations;

[TestSubject(typeof(KisService))]
public class KisServiceTest
{
    private readonly Mock<IKisApiClient> _mockKisApiClient;
    private readonly Mock<IDbContextWrapper> _mockDbContextWrapper;
    private readonly Mock<IDbTransactionWrapper> _mockDbTransaction;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IKisTokenService> _mockKisTokenService;
    private readonly Mock<ILogger<KisService>> _mockLogger;
    private readonly KisService _kisService;

    public KisServiceTest()
    {
        _mockKisApiClient = new Mock<IKisApiClient>();
        _mockDbContextWrapper = new Mock<IDbContextWrapper>();
        _mockDbTransaction = new Mock<IDbTransactionWrapper>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockKisTokenService = new Mock<IKisTokenService>();
        _mockLogger = new Mock<ILogger<KisService>>();

        _mockDbContextWrapper
            .Setup(db => db.BeginTransactionAsync())
            .ReturnsAsync(_mockDbTransaction.Object);

        _kisService = new KisService(
            _mockKisApiClient.Object,
            _mockDbContextWrapper.Object,
            _mockOrderRepository.Object,
            _mockKisTokenService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task PlaceOrderAsync_ValidOrder_ReturnsSuccessResponse()
    {
        var userDto = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            KisAppKey = "testAppKey",
            KisAppSecret = "testAppSecret",
            AccountNumber = "12345678901234"
        };

        var order = new StockOrderRequest
        {
            PDNO = "005930", // 삼성전자 종목코드
            tr_id = "VTTC0802U", // 매수 주문
            ORD_DVSN = "00", // 지정가
            ORD_QTY = "10", // 10주
            ORD_UNPR = "70000" // 70,000원
        };

        var expectedResponse = new StockOrderResponse
        {
            rt_cd = "0",
            msg_cd = "MSG_0001",
            msg = "정상처리 되었습니다.",
            output = new OrderOutput
            {
                ODNO = "123456789",
                KRX_FWDG_ORD_ORGNO = "12345",
                ORD_TMD = "102030"
            }
        };

        var savedOrder = new StockOrder(
            stockCode: "005930",
            tradeType: "VTTC0802U",
            orderType: "00",
            quantity: 10,
            price: 70000,
            user: userDto.ToEntity()
        );

        _mockKisApiClient
            .Setup(client => client.PlaceOrderAsync(It.IsAny<StockOrderRequest>(), It.IsAny<UserDto>()))
            .ReturnsAsync(expectedResponse);
        _mockOrderRepository
            .Setup(repo => repo.SaveAsync(It.IsAny<StockOrder>(), It.IsAny<UserDto>()))
            .ReturnsAsync(savedOrder);

        var result = await _kisService.PlaceOrderAsync(order, userDto);

        Assert.NotNull(result);
        Assert.Equal("0", result.rt_cd);
        Assert.Equal("123456789", result.output.ODNO);
        _mockKisApiClient.Verify(
            client => client.PlaceOrderAsync(It.IsAny<StockOrderRequest>(), It.IsAny<UserDto>()),
            Times.Once);
        _mockOrderRepository.Verify(
            repo => repo.SaveAsync(It.IsAny<StockOrder>(), It.IsAny<UserDto>()),
            Times.Once);
    }

    [Fact]
    public async Task PlaceOrderAsync_InvalidQuantity_ThrowsArgumentException()
    {
        var userDto = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            AccountNumber = "12345678901234"
        };

        var order = new StockOrderRequest
        {
            PDNO = "005930",
            tr_id = "TTTC0802U",
            ORD_DVSN = "00",
            ORD_QTY = "invalid",
            ORD_UNPR = "70000"
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _kisService.PlaceOrderAsync(order, userDto));

        Assert.Contains("유효하지 않은 수량입니다", exception.Message);
    }

    [Fact]
    public async Task GetStockBalanceAsync_ValidUser_ReturnsBalance()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            KisAppKey = "testAppKey",
            KisAppSecret = "testAppSecret",
            AccountNumber = "12345678901234"
        };

        var expectedBalance = new StockBalance
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

        _mockKisApiClient
            .Setup(client => client.GetStockBalanceAsync(It.IsAny<UserDto>()))
            .ReturnsAsync(expectedBalance);

        var result = await _kisService.GetStockBalanceAsync(userDto);

        Assert.NotNull(result);
        Assert.Single(result.Positions);
        Assert.Equal("005930", result.Positions[0].StockCode);
        Assert.Equal("삼성전자", result.Positions[0].StockName);
        Assert.Equal("10", result.Positions[0].Quantity);
        Assert.Equal("65000", result.Positions[0].AveragePrice);
        Assert.Equal("70000", result.Positions[0].CurrentPrice);
        Assert.Equal("50000", result.Positions[0].ProfitLoss);
        Assert.Equal("7.69", result.Positions[0].ProfitLossRate);

        Assert.NotNull(result.Summary);
        Assert.Equal("1000000", result.Summary.TotalDeposit);
        Assert.Equal("700000", result.Summary.StockEvaluation);
        Assert.Equal("1700000", result.Summary.TotalEvaluation);
        _mockKisApiClient.Verify(
            client => client.GetStockBalanceAsync(It.IsAny<UserDto>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserKisInfoAndTokensAsync_ValidInputs_ReturnTokenResponse()
    {
        int userId = 1;
        string appKey = "testAppKey";
        string appSecret = "testAppSecret";
        string accountNumber = "12345678901234";

        var expectedTokenResponse = new TokenResponse
        {
            AccessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
            TokenType = "Bearer",
            ExpiresIn = 86400,
        };

        _mockKisTokenService
            .Setup(service => service.GetKisTokenAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(expectedTokenResponse);

        _mockKisTokenService
            .Setup(service => service.GetWebSocketTokenAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync("web-socket-approval-key");

        _mockDbTransaction
            .Setup(t => t.CommitAsync())
            .Returns(Task.CompletedTask);

        var result = await _kisService.UpdateUserKisInfoAndTokensAsync(userId, appKey, appSecret, accountNumber);

        Assert.NotNull(result);
        Assert.Equal(expectedTokenResponse.AccessToken, result.AccessToken);
        Assert.Equal(expectedTokenResponse.TokenType, result.TokenType);
        Assert.Equal(expectedTokenResponse.ExpiresIn, result.ExpiresIn);

        _mockKisTokenService.Verify(
            service => service.GetKisTokenAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);

        _mockKisTokenService.Verify(
            service => service.GetWebSocketTokenAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);

        _mockDbTransaction.Verify(
            t => t.CommitAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserKisInfoAndTokensAsync_EmptyAppKey_ThrowsNullReferenceException()
    {
        int userId = 1;
        string appKey = "";
        string appSecret = "testAppSecret";
        string accountNumber = "12345678901234";

        await Assert.ThrowsAsync<NullReferenceException>(
            () => _kisService.UpdateUserKisInfoAndTokensAsync(userId, appKey, appSecret, accountNumber));
    }

    [Fact]
    public async Task UpdateUserKisInfoAndTokensAsync_ApiError_ThrowsExceptionAndRollsback()
    {
        int userId = 1;
        string appKey = "testAppKey";
        string appSecret = "testAppSecret";
        string accountNumber = "12345678901234";

        _mockKisTokenService
            .Setup(service => service.GetKisTokenAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("API 호출 실패", null, BadRequest));

        _mockDbTransaction
            .Setup(t => t.RollbackAsync())
            .Returns(Task.CompletedTask);

        var exception = await Assert.ThrowsAsync<Exception>(
            () => _kisService.UpdateUserKisInfoAndTokensAsync(userId, appKey, appSecret, accountNumber));

        Assert.Contains("한국투자증권 API 호출 중 오류가 발생했습니다", exception.Message);

        _mockDbTransaction.Verify(
            t => t.RollbackAsync(),
            Times.Once);
    }
}