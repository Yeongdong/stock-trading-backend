using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StockTrading.API.Controllers;
using StockTrading.API.Controllers.User;
using StockTrading.API.Services;
using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Tests.Unit.Controllers;

[TestSubject(typeof(UserController))]
public class UserControllerTest
{
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly UserController _controller;

    public UserControllerTest()
    {
        _mockUserContextService = new Mock<IUserContextService>();
        _controller = new UserController(_mockUserContextService.Object);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsOkResult()
    {
        // Arrange
        var testUser = new UserInfo
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User"
        };

        _mockUserContextService
            .Setup(x => x.GetCurrentUserAsync())
            .ReturnsAsync(testUser);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUser = Assert.IsType<UserInfo>(okResult.Value);
        Assert.Equal(testUser.Id, returnedUser.Id);
        Assert.Equal(testUser.Email, returnedUser.Email);
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserContextThrows_ThrowsException()
    {
        // Arrange
        _mockUserContextService
            .Setup(x => x.GetCurrentUserAsync())
            .ThrowsAsync(new UnauthorizedAccessException("인증 오류"));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _controller.GetCurrentUser());
    }
}