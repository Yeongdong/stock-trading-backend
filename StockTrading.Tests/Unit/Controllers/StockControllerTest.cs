using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using stock_trading_backend.Controllers;
using stock_trading_backend.Services;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.DTOs.OrderDTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace StockTrading.Tests.Unit.Controllers;

[TestSubject(typeof(StockController))]
public class StockControllerTest
{
    private readonly Mock<IKisService> _mockKisService;
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly Mock<ILogger<StockController>> _mockLogger;
    private readonly StockController _controller;
    private readonly UserDto _testUser;

    public StockControllerTest()
    {
        _mockKisService = new Mock<IKisService>();
        _mockUserContextService = new Mock<IUserContextService>();
        _mockLogger = new Mock<ILogger<StockController>>();

        _testUser = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };

        _controller = new StockController(
            _mockKisService.Object,
            _mockUserContextService.Object,
            _mockLogger.Object);

        _mockUserContextService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(_testUser);
    }

    [Fact]
    public async Task GetBalance_ReturnsOkResult()
    {
        // Arrange
        var expectedBalance = new StockBalance
        {
            Positions = new List<Position>(),
            Summary = new Summary()
        };

        _mockKisService
            .Setup(x => x.GetStockBalanceAsync(_testUser))
            .ReturnsAsync(expectedBalance);

        // Act
        var result = await _controller.GetBalance();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<StockBalance>(okResult.Value);
        Assert.Equal(expectedBalance, returnValue);
    }

    [Fact]
    public async Task PlaceOrder_ReturnsOkResult()
    {
        // Arrange
        var orderRequest = new StockOrderRequest
        {
            tr_id = "VTTC0802U",
            PDNO = "005930",
            ORD_DVSN = "00",
            ORD_QTY = "10",
            ORD_UNPR = "70000"
        };

        var expectedResponse = new StockOrderResponse
        {
            rt_cd = "0",
            msg = "정상처리 되었습니다.",
            output = new OrderOutput { ODNO = "123456" }
        };

        _mockKisService
            .Setup(x => x.PlaceOrderAsync(orderRequest, _testUser))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.PlaceOrder(orderRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<StockOrderResponse>(okResult.Value);
        Assert.Equal(expectedResponse, returnValue);
    }
}