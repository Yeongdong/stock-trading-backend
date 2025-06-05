using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StockTrading.API.Controllers;
using StockTrading.API.Services;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;
using StockTrading.Application.DTOs.Trading.Orders;
using StockTrading.Application.DTOs.Trading.Portfolio;
using StockTrading.Application.DTOs.Users;
using StockTrading.Application.Services;

namespace StockTrading.Tests.Unit.Controllers;

[TestSubject(typeof(StockController))]
public class StockControllerTest
{
    private readonly Mock<IOrderService> _mockKisOrderService;
    private readonly Mock<IBalanceService> _mockKisBalanceService;
    private readonly Mock<IStockService> _mockStockService;
    private readonly Mock<ICurrentPriceService> _mockCurrentPriceService;
    private readonly Mock<IStockCacheService> _mockStockCacheService;
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly Mock<ILogger<StockController>> _mockLogger;
    private readonly StockController _controller;
    private readonly UserInfo _testUser;

    public StockControllerTest()
    {
        _mockKisOrderService = new Mock<IOrderService>();
        _mockKisBalanceService = new Mock<IBalanceService>();
        _mockStockService = new Mock<IStockService>();
        _mockCurrentPriceService = new Mock<ICurrentPriceService>();
        _mockStockCacheService = new Mock<IStockCacheService>();
        _mockUserContextService = new Mock<IUserContextService>();
        _mockLogger = new Mock<ILogger<StockController>>();

        _testUser = new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };

        _controller = new StockController(
            _mockKisOrderService.Object,
            _mockKisBalanceService.Object, 
            _mockStockService.Object,
            _mockCurrentPriceService.Object,
            _mockStockCacheService.Object,
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
        var expectedBalance = new AccountBalance
        {
            Positions = new List<KisPositionResponse>(),
            Summary = new KisAccountSummaryResponse()
        };

        _mockKisBalanceService
            .Setup(x => x.GetStockBalanceAsync(_testUser))
            .ReturnsAsync(expectedBalance);

        // Act
        var result = await _controller.GetBalance();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<AccountBalance>(okResult.Value);
        Assert.Equal(expectedBalance, returnValue);
    }

    [Fact]
    public async Task PlaceOrder_ReturnsOkResult()
    {
        // Arrange
        var orderRequest = new OrderRequest
        {
            tr_id = "VTTC0802U",
            PDNO = "005930",
            ORD_DVSN = "00",
            ORD_QTY = "10",
            ORD_UNPR = "70000"
        };

        var expectedResponse = new OrderResponse
        {
            ReturnCode = "0",
            Message = "정상처리 되었습니다.",
            Output =
                new KisOrderData
                {
                    KrxForwardOrderOrgNo = "91252", // 예시 거래소코드
                    OrderNumber = "0000117057", // 예시 주문번호  
                    OrderTime = "121052" // 예시 주문시간 (HHMMSS)
                }
        };

        _mockKisOrderService
            .Setup(x => x.PlaceOrderAsync(orderRequest, _testUser))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.PlaceOrder(orderRequest);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<OrderResponse>(okResult.Value);
        Assert.Equal(expectedResponse, returnValue);
    }

    [Fact]
    public async Task PlaceOrder_InvalidModel_ReturnsBadRequest()
    {
        // Arrange
        var invalidOrder = new OrderRequest
        {
            // 필수 필드들을 비워둠
            PDNO = "12345", // 잘못된 형식 (5자리)
            ORD_QTY = "0", // 0은 유효하지 않음
            ORD_UNPR = "-100" // 음수는 유효하지 않음
        };

        _controller.ModelState.AddModelError("PDNO", "종목코드는 6자리 숫자여야 합니다.");
        _controller.ModelState.AddModelError("ORD_QTY", "주문수량은 1 이상이어야 합니다.");

        // Act
        var result = await _controller.PlaceOrder(invalidOrder);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}