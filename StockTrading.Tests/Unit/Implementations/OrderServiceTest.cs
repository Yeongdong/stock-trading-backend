using JetBrains.Annotations;
using Moq;
using StockTrading.Application.Common.Interfaces;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.ExternalServices;
using StockTrading.Application.Features.Trading.DTOs.Orders;
using StockTrading.Application.Features.Trading.Repositories;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Entities;
using StockTrading.Domain.Exceptions.Authentication;
using StockTrading.Infrastructure.Services.Trading;

namespace StockTrading.Tests.Unit.Implementations;

[TestSubject(typeof(OrderService))]
public class OrderServiceTest
{
    private readonly Mock<IKisOrderApiClient> _mockKisOrderApiClient;
    private readonly Mock<IDbContextWrapper> _mockDbContextWrapper;
    private readonly Mock<IDbTransactionWrapper> _mockDbTransaction;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly OrderService _orderService;

    public OrderServiceTest()
    {
        _mockKisOrderApiClient = new Mock<IKisOrderApiClient>();
        _mockDbContextWrapper = new Mock<IDbContextWrapper>();
        _mockDbTransaction = new Mock<IDbTransactionWrapper>();
        _mockOrderRepository = new Mock<IOrderRepository>();

        _mockDbContextWrapper
            .Setup(db => db.BeginTransactionAsync())
            .ReturnsAsync(_mockDbTransaction.Object);

        _orderService = new OrderService(_mockKisOrderApiClient.Object, _mockDbContextWrapper.Object,
            _mockOrderRepository.Object);
    }

    [Fact]
    public async Task PlaceOrderAsync_ValidOrder_ReturnsSuccessResponse()
    {
        var userDto = CreateTestUser();
        var order = CreateTestOrder();
        var expectedResponse = CreateTestOrderResponse();

        _mockKisOrderApiClient
            .Setup(client => client.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<UserInfo>()))
            .ReturnsAsync(expectedResponse);
        _mockOrderRepository
            .Setup(repo => repo.AddAsync(It.IsAny<StockOrder>()))
            .ReturnsAsync(It.IsAny<StockOrder>);

        var result = await _orderService.PlaceOrderAsync(order, userDto);

        Assert.NotNull(result);
        Assert.Equal("0", result.ReturnCode);
        Assert.Equal("123456789", result.Output.OrderNumber);

        _mockKisOrderApiClient.Verify(client => client.PlaceOrderAsync(order, userDto), Times.Once);
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
            _orderService.PlaceOrderAsync(null, userDto));
    }

    [Fact]
    public async Task PlaceOrderAsync_UserWithoutKisAppKey_ThrowsArgumentException()
    {
        // Arrange
        var userDto = CreateTestUser();
        userDto.KisAppKey = null;
        var order = CreateTestOrder();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _orderService.PlaceOrderAsync(order, userDto));

        Assert.Contains("KIS 앱 키가 설정되지 않았습니다.", exception.Message);
    }

    [Fact]
    public async Task PlaceOrderAsync_ExpiredToken_ThrowsKisTokenExpiredException()
    {
        // Arrange
        var userDto = CreateTestUser();
        userDto.KisToken.ExpiresIn = DateTime.UtcNow.AddMinutes(-1); // 만료된 토큰
        var order = CreateTestOrder();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KisTokenExpiredException>(() =>
            _orderService.PlaceOrderAsync(order, userDto));

        Assert.Equal("KIS 액세스 토큰이 만료되었습니다.", exception.Message);
    }

    [Fact]
    public async Task PlaceOrderAsync_ApiCallFails_ThrowsException()
    {
        // Arrange
        var userDto = CreateTestUser();
        var order = CreateTestOrder();

        _mockKisOrderApiClient
            .Setup(client => client.PlaceOrderAsync(It.IsAny<OrderRequest>(), It.IsAny<UserInfo>()))
            .ThrowsAsync(new HttpRequestException("API 호출 실패"));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _orderService.PlaceOrderAsync(order, userDto));

        _mockDbContextWrapper.Verify(db => db.BeginTransactionAsync(), Times.Once);
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
            ReturnCode = "0",
            MessageCode = "MSG_0001",
            Message = "정상처리 되었습니다.",
            Output =
                new KisOrderData
                {
                    OrderNumber = "123456789",
                    KrxForwardOrderOrgNo = "12345",
                    OrderTime = "102030"
                }
        };
    }
}