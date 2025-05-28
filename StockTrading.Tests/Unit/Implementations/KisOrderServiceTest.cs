using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Repositories;
using StockTrading.Application.Services;
using StockTrading.Domain.Entities;
using StockTrading.Infrastructure.Services;

namespace StockTrading.Tests.Unit.Implementations;

[TestSubject(typeof(KisOrderService))]
public class KisOrderServiceTest
{
    private readonly Mock<IKisApiClient> _mockKisApiClient;
    private readonly Mock<IDbContextWrapper> _mockDbContextWrapper;
    private readonly Mock<IDbTransactionWrapper> _mockDbTransaction;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<ILogger<KisOrderService>> _mockLogger;
    private readonly KisOrderService _kisOrderService;

    public KisOrderServiceTest()
    {
        _mockKisApiClient = new Mock<IKisApiClient>();
        _mockDbContextWrapper = new Mock<IDbContextWrapper>();
        _mockDbTransaction = new Mock<IDbTransactionWrapper>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockLogger = new Mock<ILogger<KisOrderService>>();

        _mockDbContextWrapper
            .Setup(db => db.BeginTransactionAsync())
            .ReturnsAsync(_mockDbTransaction.Object);

        _kisOrderService = new KisOrderService(
            _mockKisApiClient.Object,
            _mockDbContextWrapper.Object,
            _mockOrderRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task PlaceOrderAsync_ValidOrder_ReturnsSuccessResponse()
    {
        var userDto = CreateTestUser();
        var order = CreateTestOrder();
        var expectedResponse = CreateTestOrderResponse();

        _mockKisApiClient
            .Setup(client => client.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<UserInfo>()))
            .ReturnsAsync(expectedResponse);
        _mockOrderRepository
            .Setup(repo => repo.AddAsync(It.IsAny<StockOrder>()))
            .ReturnsAsync(It.IsAny<StockOrder>);

        var result = await _kisOrderService.PlaceOrderAsync(order, userDto);

        Assert.NotNull(result);
        Assert.Equal("0", result.rt_cd);
        Assert.Equal("123456789", result.Info.ODNO);

        _mockKisApiClient.Verify(client => client.PlaceOrderAsync(order, userDto), Times.Once);
        _mockOrderRepository.Verify(repo => repo.AddAsync(It.IsAny<StockOrder>()), Times.Once);
        _mockDbTransaction.Verify(t => t.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task PlaceOrderAsync_NullOrder_ThrowsArgumentNullException()
    {
        // Arrange
        var userDto = CreateTestUser();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _kisOrderService.PlaceOrderAsync(null, userDto));
    }

    [Fact]
    public async Task PlaceOrderAsync_UserWithoutKisAppKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var userDto = CreateTestUser();
        userDto.KisAppKey = null;
        var order = CreateTestOrder();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _kisOrderService.PlaceOrderAsync(order, userDto));

        Assert.Equal("KIS 앱 키가 설정되지 않았습니다.", exception.Message);
    }

    [Fact]
    public async Task PlaceOrderAsync_ExpiredToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var userDto = CreateTestUser();
        userDto.KisToken.ExpiresIn = DateTime.UtcNow.AddMinutes(-1); // 만료된 토큰
        var order = CreateTestOrder();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _kisOrderService.PlaceOrderAsync(order, userDto));

        Assert.Equal("KIS 액세스 토큰이 만료되었습니다. 토큰을 재발급받아주세요.", exception.Message);
    }

    [Fact]
    public async Task PlaceOrderAsync_ApiCallFails_ThrowsException()
    {
        // Arrange
        var userDto = CreateTestUser();
        var order = CreateTestOrder();

        _mockKisApiClient
            .Setup(client => client.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<UserInfo>()))
            .ThrowsAsync(new HttpRequestException("API 호출 실패"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _kisOrderService.PlaceOrderAsync(order, userDto));

        // 트랜잭션이 시작되었는지 확인
        _mockDbContextWrapper.Verify(db => db.BeginTransactionAsync(), Times.Once);
        // 주문 저장은 호출되지 않아야 함
        _mockOrderRepository.Verify(repo => repo.AddAsync(It.IsAny<StockOrder>()), Times.Never);
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

    private static OrderRequest CreateTestOrder()
    {
        return new OrderRequest
        {
            PDNO = "005930",
            tr_id = "VTTC0802U",
            ORD_DVSN = "00",
            ORD_QTY = "10",
            ORD_UNPR = "70000"
        };
    }

    private static OrderResponse CreateTestOrderResponse()
    {
        return new OrderResponse
        {
            rt_cd = "0",
            msg_cd = "MSG_0001",
            msg = "정상처리 되었습니다.",
            Info = new OrderInfo
            {
                ODNO = "123456789",
                KRX_FWDG_ORD_ORGNO = "12345",
                ORD_TMD = "102030"
            }
        };
    }
}