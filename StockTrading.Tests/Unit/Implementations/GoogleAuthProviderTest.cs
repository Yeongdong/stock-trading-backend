using System.Security.Claims;
using JetBrains.Annotations;
using StockTrading.Infrastructure.Implementations;

namespace StockTrading.Tests.Unit.Implementations;

[TestSubject(typeof(GoogleAuthProvider))]
public class GoogleAuthProviderTest
{
    private readonly GoogleAuthProvider _googleAuthProvider;

    public GoogleAuthProviderTest()
    {
        _googleAuthProvider = new GoogleAuthProvider();
    }

    [Fact]
    public async Task GetUserInfoAsync_WithValidPrincipal_ShouldReturnGoogleUserInfo()
    {
        var testEmail = "test@example.com";
        var testName = "Test User";

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, testEmail),
            new Claim(ClaimTypes.Name, testName)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var result = await _googleAuthProvider.GetUserInfoAsync(principal);

        Assert.NotNull(result);
        Assert.Equal(testEmail, result.Email);
        Assert.Equal(testName, result.Name);
    }

    [Fact]
    public async Task GetUserInfoAsync_WithMultipleEmailClaims_ShouldUseFirstValue()
    {
        var testEmail1 = "first@example.com";
        var testEmail2 = "second@example.com";
        var testName = "Test User";

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, testEmail1),
            new Claim(ClaimTypes.Email, testEmail2),
            new Claim(ClaimTypes.Name, testName)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        var result = await _googleAuthProvider.GetUserInfoAsync(principal);

        Assert.NotNull(result);
        Assert.Equal(testEmail1, result.Email);
        Assert.Equal(testName, result.Name);
    }

    [Fact]
    public async Task GetUserInfoAsync_WithMissingEmailClaim_ShouldThrowNullReferenceException()
    {
        var testName = "Test User";

        // 이메일 클레임 없이 생성
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, testName)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        await Assert.ThrowsAsync<NullReferenceException>(() =>
            _googleAuthProvider.GetUserInfoAsync(principal));
    }

    [Fact]
    public async Task GetUserInfoAsync_WithMissingNameClaim_ShouldThrowNullReferenceException()
    {
        var testEmail = "test@example.com";

        // 이름 클레임 없이 생성
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, testEmail)
        };

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);

        await Assert.ThrowsAsync<NullReferenceException>(() =>
            _googleAuthProvider.GetUserInfoAsync(principal));
    }
    
    [Fact]
    public async Task GetUserInfoAsync_WithNullPrincipal_ShouldThrowArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _googleAuthProvider.GetUserInfoAsync(null));
    }
}