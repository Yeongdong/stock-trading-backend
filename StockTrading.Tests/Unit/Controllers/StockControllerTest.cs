using System.Security.Claims;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using stock_trading_backend.Controllers;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.DTOs.OrderDTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace StockTrading.Tests.Unit.Controllers;

[TestSubject(typeof(StockController))]
public class StockControllerTest
{
    private readonly Mock<IKisService> _mockKisService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<StockController>> _mockLogger;
    private readonly StockController _controller;
    private readonly UserDto _testUser;

    public StockControllerTest()
    {
        _mockKisService = new Mock<IKisService>();
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<StockController>>();

        _testUser = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };

        _controller = new StockController(
            _mockKisService.Object,
            _mockUserService.Object,
            _mockLogger.Object);

        SetupUserClaims();
    }

    private void SetupUserClaims()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, _testUser.Email)
        };

        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _mockUserService
            .Setup(x => x.GetUserByEmailAsync(_testUser.Email))
            .ReturnsAsync(_testUser);
    }

    [Fact]
    public async Task GetBalance_WithValidUser_ReturnsOkResult()
    {
        var expectedBalance = new StockBalance
        {
            Positions = new List<Position>(),
            Summary = new Summary()
        };

        _mockKisService
            .Setup(x => x.GetStockBalanceAsync(_testUser))
            .ReturnsAsync(expectedBalance);

        var result = await _controller.GetBalance();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<StockBalance>(okResult.Value);
        Assert.Equal(expectedBalance, returnValue);
        _mockKisService.Verify(x => x.GetStockBalanceAsync(_testUser), Times.Once);
    }

    [Fact]
    public async Task GetBalance_WhenExceptionOccurs_Returns500StatusCode()
    {
        _mockKisService
            .Setup(x => x.GetStockBalanceAsync(_testUser))
            .ThrowsAsync(new Exception("테스트 예외"));

        var result = await _controller.GetBalance();

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("잔고 조회 중 오류 발생", statusCodeResult.Value);
    }

    [Fact]
    public async Task PlaceOrder_WithValidRequest_ReturnsOkResult()
    {
        var orderRequest = new StockOrderRequest
        {
            tr_id = "VTTC0802U",
            PDNO = "005930", // 삼성전자 종목코드
            ORD_DVSN = "00", // 지정가
            ORD_QTY = "10", // 주문수량
            ORD_UNPR = "70000" // 주문단가
        };

        var expectedResponse = new StockOrderResponse
        {
            rt_cd = "0",
            msg_cd = "APBK0013",
            msg = "정상처리 되었습니다.",
            output = new OrderOutput()
        };

        _mockKisService
            .Setup(x => x.PlaceOrderAsync(orderRequest, _testUser))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.PlaceOrder(orderRequest);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<StockOrderResponse>(okResult.Value);
        Assert.Equal(expectedResponse, returnValue);
        _mockKisService.Verify(x => x.PlaceOrderAsync(orderRequest, _testUser), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("주문 시작")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)
            ),
            Times.Once
        );
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("주문 완료")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task PlaceOrder_WhenExceptionOccurs_Returns500StatusCode()
    {
        var orderRequest = new StockOrderRequest
        {
            tr_id = "VTTC0802U",
            PDNO = "005930",
            ORD_DVSN = "00",
            ORD_QTY = "10",
            ORD_UNPR = "70000"
        };

        _mockKisService
            .Setup(x => x.PlaceOrderAsync(orderRequest, _testUser))
            .ThrowsAsync(new Exception("테스트 예외"));

        var result = await _controller.PlaceOrder(orderRequest);

        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("주문 실패", statusCodeResult.Value);
    }
    
    [Fact]
    public async Task GetUser_WithValidClaims_ReturnsUser()
    {
        // GetUser 메서드는 private이므로 간접적으로 테스트
        // GetBalance 메서드를 호출하면 내부적으로 GetUser를 사용
    
        var expectedBalance = new StockBalance
        {
            Positions = new List<Position>(),
            Summary = new Summary()
        };
    
        _mockKisService
            .Setup(x => x.GetStockBalanceAsync(_testUser))
            .ReturnsAsync(expectedBalance);
    
        var result = await _controller.GetBalance();
    
        // GetUser 메서드가 예외를 발생시키지 않고 정상적으로 처리되었는지 확인
        Assert.IsType<OkObjectResult>(result.Result);
        // GetUserByEmailAsync가 호출되었는지 확인
        _mockUserService.Verify(x => x.GetUserByEmailAsync(_testUser.Email), Times.Once);
    }

    [Fact]
    public async Task GetBalance_WithoutClaims_ReturnsUnauthorized()
    {
        // Arrange - 클레임이 없는 상황을 만들기 위해 새로운 HttpContext를 설정
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        var result = await _controller.GetBalance();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal("사용자 인증 정보가 유효하지 않습니다.", unauthorizedResult.Value);
    }

    [Fact]
    public async Task GetBalance_WithNullUserService_ReturnsNotFound()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Email, _testUser.Email) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _mockUserService
            .Setup(x => x.GetUserByEmailAsync(_testUser.Email))
            .ReturnsAsync((UserDto)null);

        // Act
        var result = await _controller.GetBalance();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("사용자 정보를 찾을 수 없습니다.", notFoundResult.Value);
    }

    [Fact]
    public async Task PlaceOrder_WithoutClaims_ReturnsUnauthorized()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var orderRequest = new StockOrderRequest
        {
            tr_id = "VTTC0802U",
            PDNO = "005930",
            ORD_DVSN = "00",
            ORD_QTY = "10",
            ORD_UNPR = "70000"
        };

        // Act
        var result = await _controller.PlaceOrder(orderRequest);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal("사용자 인증 정보가 유효하지 않습니다.", unauthorizedResult.Value);
    }

    [Fact]
    public async Task PlaceOrder_WithNullUserService_ReturnsNotFound()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Email, _testUser.Email) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _mockUserService
            .Setup(x => x.GetUserByEmailAsync(_testUser.Email))
            .ReturnsAsync((UserDto)null);

        var orderRequest = new StockOrderRequest
        {
            tr_id = "VTTC0802U",
            PDNO = "005930",
            ORD_DVSN = "00",
            ORD_QTY = "10",
            ORD_UNPR = "70000"
        };

        // Act
        var result = await _controller.PlaceOrder(orderRequest);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("사용자 정보를 찾을 수 없습니다.", notFoundResult.Value);
    }

    [Fact]
    public async Task GetBalance_WhenUserServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Email, _testUser.Email) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        _mockUserService
            .Setup(x => x.GetUserByEmailAsync(_testUser.Email))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _controller.GetBalance();

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("사용자 정보 조회 실패", statusCodeResult.Value);
    }
}