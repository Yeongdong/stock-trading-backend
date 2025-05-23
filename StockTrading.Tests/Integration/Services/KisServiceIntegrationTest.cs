using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.DTOs.OrderDTOs;
using StockTrading.DataAccess.Repositories;
using StockTrading.DataAccess.Services.Interfaces;
using StockTrading.Infrastructure.ExternalServices.Interfaces;
using StockTrading.Infrastructure.Implementations;
using StockTrading.Infrastructure.Interfaces;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Integration.Services;

public class KisServiceIntegrationTest
{
    private readonly Mock<IKisApiClient> _mockKisApiClient;
    private readonly Mock<IDbContextWrapper> _mockDbContextWrapper;
    private readonly Mock<IDbTransactionWrapper> _mockTransaction;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IKisTokenService> _mockKisTokenService;
    private readonly ILogger<KisService> _logger;
    private readonly KisService _kisService;

    public KisServiceIntegrationTest()
    {
        _mockKisApiClient = new Mock<IKisApiClient>();
        _mockDbContextWrapper = new Mock<IDbContextWrapper>();
        _mockTransaction = new Mock<IDbTransactionWrapper>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockKisTokenService = new Mock<IKisTokenService>();
        _logger = new NullLogger<KisService>();

        _mockDbContextWrapper
            .Setup(db => db.BeginTransactionAsync())
            .ReturnsAsync(_mockTransaction.Object);

        _kisService = new KisService(
            _mockKisApiClient.Object,
            _mockDbContextWrapper.Object,
            _mockOrderRepository.Object,
            _mockKisTokenService.Object,
            _logger
        );
    }

    [Fact]
    public async Task PlaceOrderAsync_ValidOrder_ReturnsOrderResponse()
    {
        var userDto = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            KisAppKey = "test_app_key",
            KisAppSecret = "test_app_secret",
            AccountNumber = "123456789"
        };

        var orderRequest = new StockOrderRequest
        {
            PDNO = "005930", // 삼성전자 종목코드
            tr_id = "VTTC0802U", // 매수
            ORD_DVSN = "00", // 지정가
            ORD_QTY = "10", // 수량
            ORD_UNPR = "70000" // 가격
        };

        var expectedResponse = new StockOrderResponse
        {
            rt_cd = "0",
            msg_cd = "MSG0000",
            msg = "정상처리 되었습니다.",
            output = new OrderOutput
            {
                ODNO = "123456789", // 주문번호
                KRX_FWDG_ORD_ORGNO = "12345",
                ORD_TMD = "102030" // 주문시간
            }
        };

        var stockOrder = new StockOrder(
            "005930",
            "VTTC0802U",
            "00",
            10,
            70000,
            userDto.ToEntity()
        );

        // API 클라이언트 응답 설정
        _mockKisApiClient
            .Setup(client => client.PlaceOrderAsync(orderRequest, userDto))
            .ReturnsAsync(expectedResponse);

        // 주문 저장소 설정
        _mockOrderRepository
            .Setup(repo => repo.SaveAsync(It.IsAny<StockOrder>(), userDto))
            .ReturnsAsync(stockOrder);

        var result = await _kisService.PlaceOrderAsync(orderRequest, userDto);

        Assert.NotNull(result);
        Assert.Equal("0", result.rt_cd);
        Assert.Equal("MSG0000", result.msg_cd);
        Assert.Equal("정상처리 되었습니다.", result.msg);
        Assert.NotNull(result.output);
        Assert.Equal("123456789", result.output.ODNO);

        _mockKisApiClient.Verify(
            client => client.PlaceOrderAsync(orderRequest, userDto),
            Times.Once);
        _mockOrderRepository.Verify(
            repo => repo.SaveAsync(It.IsAny<StockOrder>(), userDto),
            Times.Once);
    }

    [Fact]
    public async Task PlaceOrderAsync_InvalidOrder_ThrowsArgumentException()
    {
        var userDto = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            KisAppKey = "test_app_key",
            KisAppSecret = "test_app_secret",
            AccountNumber = "123456789"
        };

        var invalidOrderRequest = new StockOrderRequest
        {
            PDNO = "005930", // 삼성전자 종목코드
            tr_id = "VTTC0802U", // 매수
            ORD_DVSN = "00", // 지정가
            ORD_QTY = "invalid", // 잘못된 수량
            ORD_UNPR = "70000" // 가격
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _kisService.PlaceOrderAsync(invalidOrderRequest, userDto));

        _mockKisApiClient.Verify(
            client => client.PlaceOrderAsync(It.IsAny<StockOrderRequest>(), It.IsAny<UserDto>()),
            Times.Never);
        _mockOrderRepository.Verify(
            repo => repo.SaveAsync(It.IsAny<StockOrder>(), It.IsAny<UserDto>()),
            Times.Never);
    }

    [Fact]
    public async Task GetStockBalanceAsync_ValidUser_ReturnsStockBalance()
    {
        var userDto = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            KisAppKey = "test_app_key",
            KisAppSecret = "test_app_secret",
            AccountNumber = "123456789"
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
                    AveragePrice = "70000",
                    CurrentPrice = "72000",
                    ProfitLoss = "20000",
                    ProfitLossRate = "2.86"
                }
            },
            Summary = new Summary
            {
                TotalDeposit = "1000000",
                StockEvaluation = "720000",
                TotalEvaluation = "1720000"
            }
        };

        _mockKisApiClient
            .Setup(client => client.GetStockBalanceAsync(userDto))
            .ReturnsAsync(expectedBalance);

        var result = await _kisService.GetStockBalanceAsync(userDto);

        Assert.NotNull(result);
        Assert.NotNull(result.Positions);
        Assert.NotNull(result.Summary);
        Assert.Single(result.Positions);
        Assert.Equal("005930", result.Positions[0].StockCode);
        Assert.Equal("삼성전자", result.Positions[0].StockName);
        Assert.Equal("10", result.Positions[0].Quantity);
        Assert.Equal("1000000", result.Summary.TotalDeposit);
        Assert.Equal("720000", result.Summary.StockEvaluation);
        Assert.Equal("1720000", result.Summary.TotalEvaluation);

        _mockKisApiClient.Verify(
            client => client.GetStockBalanceAsync(userDto),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserKisInfoAndTokensAsync_ValidInputs_ReturnsTokenResponse()
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

        // 토큰 서비스 응답 설정
        _mockKisTokenService
            .Setup(service => service.GetKisTokenAsync(userId, appKey, appSecret, accountNumber))
            .ReturnsAsync(tokenResponse);

        _mockKisTokenService
            .Setup(service => service.GetWebSocketTokenAsync(userId, appKey, appSecret))
            .ReturnsAsync("test_websocket_token");

        // 트랜잭션 설정
        _mockTransaction
            .Setup(t => t.CommitAsync())
            .Returns(Task.CompletedTask);

        var result = await _kisService.UpdateUserKisInfoAndTokensAsync(userId, appKey, appSecret, accountNumber);

        Assert.NotNull(result);
        Assert.Equal(tokenResponse.AccessToken, result.AccessToken);
        Assert.Equal(tokenResponse.TokenType, result.TokenType);
        Assert.Equal(tokenResponse.ExpiresIn, result.ExpiresIn);

        _mockKisTokenService.Verify(
            service => service.GetKisTokenAsync(userId, appKey, appSecret, accountNumber),
            Times.Once);
        _mockKisTokenService.Verify(
            service => service.GetWebSocketTokenAsync(userId, appKey, appSecret),
            Times.Once);
        _mockTransaction.Verify(
            t => t.CommitAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserKisInfoAndTokensAsync_EmptyCredentials_ThrowsNullReferenceException()
    {
        int userId = 1;
        string emptyAppKey = "";
        string appSecret = "test_app_secret";
        string accountNumber = "123456789";

        await Assert.ThrowsAsync<NullReferenceException>(() =>
            _kisService.UpdateUserKisInfoAndTokensAsync(userId, emptyAppKey, appSecret, accountNumber));

        _mockKisTokenService.Verify(
            service => service.GetWebSocketTokenAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateUserKisInfoAndTokensAsync_TokenServiceException_RollsBackTransaction()
    {
        int userId = 1;
        string appKey = "test_app_key";
        string appSecret = "test_app_secret";
        string accountNumber = "123456789";

        // 토큰 서비스 예외 설정
        _mockKisTokenService
            .Setup(service => service.GetKisTokenAsync(userId, appKey, appSecret, accountNumber))
            .ThrowsAsync(new HttpRequestException("API 호출 실패"));

        // 트랜잭션 설정
        _mockTransaction
            .Setup(t => t.RollbackAsync())
            .Returns(Task.CompletedTask);

        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _kisService.UpdateUserKisInfoAndTokensAsync(userId, appKey, appSecret, accountNumber));

        Assert.Contains("한국투자증권 API 호출 중 오류가 발생했습니다", exception.Message);
        _mockTransaction.Verify(
            t => t.RollbackAsync(), Times.Once);
        _mockTransaction.Verify(
            t => t.CommitAsync(), Times.Never);
    }
}