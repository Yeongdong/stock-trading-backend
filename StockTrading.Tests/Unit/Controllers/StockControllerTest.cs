using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.API.Controllers;
using StockTrading.API.Services;
using StockTrading.Application.DTOs.Common;
using StockTrading.Application.DTOs.Orders;
using StockTrading.Application.DTOs.Stocks;
using StockTrading.Application.Services;

namespace StockTrading.Tests.Unit.Controllers;

[TestSubject(typeof(StockController))]
public class StockControllerTest
{
    private readonly Mock<IKisOrderService> _mockKisOrderService;
    private readonly Mock<IKisBalanceService> _mockKisBalanceService;
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly Mock<ILogger<StockController>> _mockLogger;
    private readonly StockController _controller;
    private readonly UserDto _testUser;

    public StockControllerTest()
    {
        _mockKisOrderService = new Mock<IKisOrderService>();
        _mockKisBalanceService = new Mock<IKisBalanceService>();
        _mockUserContextService = new Mock<IUserContextService>();
        _mockLogger = new Mock<ILogger<StockController>>();

        _testUser = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };

        _controller = new StockController(
            _mockKisOrderService.Object,
            _mockKisBalanceService.Object,
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

        _mockKisBalanceService
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
            ORD_QTY = 10,
            ORD_UNPR = 70000
        };

        var expectedResponse = new StockOrderResponse
        {
            rt_cd = "0",
            msg = "정상처리 되었습니다.",
            output = new OrderOutput { ODNO = "123456" }
        };

        _mockKisOrderService
            .Setup(x => x.PlaceOrderAsync(orderRequest, _testUser))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.PlaceOrder(orderRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<StockOrderResponse>(okResult.Value);
        Assert.Equal(expectedResponse, returnValue);
    }

    [Fact]
    public async Task PlaceOrder_InvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var invalidOrder = new StockOrderRequest
        {
            // 필수 필드들을 비워둠
            PDNO = "12345", // 잘못된 형식 (5자리)
            ORD_QTY = 0, // 0은 유효하지 않음
            ORD_UNPR = -100 // 음수는 유효하지 않음
        };

        _controller.ModelState.AddModelError("PDNO", "종목코드는 6자리 숫자여야 합니다.");
        _controller.ModelState.AddModelError("ORD_QTY", "주문수량은 1 이상이어야 합니다.");

        // Act
        var result = await _controller.PlaceOrder(invalidOrder);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}