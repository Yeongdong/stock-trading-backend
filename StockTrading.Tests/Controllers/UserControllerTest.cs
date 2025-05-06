using System.Security.Claims;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using stock_trading_backend.Controllers;
using StockTrading.DataAccess.DTOs;
using StockTrading.DataAccess.Services.Interfaces;

namespace StockTrading.Tests.Controllers;

[TestSubject(typeof(UserController))]
public class UserControllerTest
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly UserController _controller;

    public UserControllerTest()
    {
        _mockUserService = new Mock<IUserService>();
        _controller = new UserController(_mockUserService.Object);
    }

    private void SetupUserContext(string email = null)
    {
        var identity = new ClaimsIdentity();

        if (!string.IsNullOrEmpty(email))
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, email));
        }

        var user = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetCurrentUser_WithValidEmail_ReturnsOkResult()
    {
        var testEmail = "test@example.com";
        var testUser = new UserDto
        {
            Id = 1,
            Email = testEmail,
            Name = "Test User",
            KisAppKey = "test-key",
            KisAppSecret = "test-secret",
            AccountNumber = "12345678",
            WebSocketToken = "test-token"
        };

        SetupUserContext(testEmail);
        _mockUserService
            .Setup(service => service.GetUserByEmailAsync(testEmail))
            .ReturnsAsync(testUser);

        var result = await _controller.GetCurrentUser();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal(testUser.Id, returnedUser.Id);
        Assert.Equal(testUser.Email, returnedUser.Email);
        Assert.Equal(testUser.Name, returnedUser.Name);
        _mockUserService.Verify(service => service.GetUserByEmailAsync(testEmail), Times.Once);
    }
    
    [Fact]
    public async Task GetCurrentUser_WithoutEmail_ReturnsUnauthorized()
    {
        SetupUserContext(); // 이메일 없이 설정
    
        var result = await _controller.GetCurrentUser();
    
        Assert.IsType<UnauthorizedResult>(result);
        _mockUserService.Verify(service => service.GetUserByEmailAsync(It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task GetCurrentUser_UserNotFound_ReturnsNotFound()
    {
        var testEmail = "nonexistent@test.com";
    
        SetupUserContext(testEmail);
        _mockUserService
            .Setup(service => service.GetUserByEmailAsync(testEmail))
            .ReturnsAsync((UserDto)null);
    
        var result = await _controller.GetCurrentUser();
    
        Assert.IsType<NotFoundResult>(result);
        _mockUserService.Verify(service => service.GetUserByEmailAsync(testEmail), Times.Once);
    }
    
    [Fact]
    public async Task GetCurrentUser_ServiceThrowsException_ReturnsInternalServerError()
    {
        var testEmail = "test@example.com";
    
        SetupUserContext(testEmail);
        _mockUserService
            .Setup(service => service.GetUserByEmailAsync(testEmail))
            .ThrowsAsync(new Exception("Test exception"));
    
        var result = await _controller.GetCurrentUser();
    
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("Internal server error", statusCodeResult.Value);
        _mockUserService.Verify(service => service.GetUserByEmailAsync(testEmail), Times.Once);
    }
}