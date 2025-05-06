using Microsoft.EntityFrameworkCore;
using StockTrading.Infrastructure.Repositories;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Integration.Repositories
{
    public class UserRepositoryIntegrationTest : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserRepository _repository;

        public UserRepositoryIntegrationTest()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new UserRepository(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GetByGoogleIdAsync_ExistingUser_ReturnsUser()
        {
            var googleId = "google_id_123";
            var user = new User
            {
                Email = "google_test@example.com",
                Name = "Google Test User",
                GoogleId = googleId,
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByGoogleIdAsync(googleId);

            Assert.NotNull(result);
            Assert.Equal(googleId, result.GoogleId);
            Assert.Equal("google_test@example.com", result.Email);
            Assert.Equal("Google Test User", result.Name);
        }

        [Fact]
        public async Task GetByGoogleIdAsync_NonExistentUser_ReturnsNull()
        {
            var nonExistentGoogleId = "non_existent_google_id";

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repository.GetByGoogleIdAsync(nonExistentGoogleId));
        }

        [Fact]
        public async Task AddAsync_NewUser_SavesUserAndReturnsWithId()
        {
            var newUser = new User
            {
                Email = "new_user@example.com",
                Name = "New User",
                GoogleId = "new_google_id",
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            var result = await _repository.AddAsync(newUser);

            Assert.NotNull(result);
            Assert.NotEqual(0, result.Id);

            var userInDb = await _context.Users.FindAsync(result.Id);
            Assert.NotNull(userInDb);
            Assert.Equal("new_user@example.com", userInDb.Email);
            Assert.Equal("New User", userInDb.Name);
            Assert.Equal("new_google_id", userInDb.GoogleId);
        }

        [Fact]
        public async Task GetByEmailAsync_ExistingUser_ReturnsUser()
        {
            var email = "email_test@example.com";
            var user = new User
            {
                Email = email,
                Name = "Email Test User",
                GoogleId = "email_google_id",
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByEmailAsync(email);

            Assert.NotNull(result);
            Assert.Equal(email, result.Email);
            Assert.Equal("Email Test User", result.Name);
            Assert.Equal("email_google_id", result.GoogleId);
        }

        [Fact]
        public async Task GetByEmailAsync_WithKisToken_ReturnsUserWithToken()
        {
            var email = "token_test@example.com";
            var user = new User
            {
                Email = email,
                Name = "Token Test User",
                GoogleId = "token_google_id",
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var kisToken = new KisToken
            {
                UserId = user.Id,
                AccessToken = "test_access_token",
                ExpiresIn = DateTime.UtcNow.AddHours(1),
                TokenType = "Bearer"
            };

            _context.KisTokens.Add(kisToken);
            await _context.SaveChangesAsync();

            var result = await _repository.GetByEmailAsync(email);

            Assert.NotNull(result);
            Assert.Equal(email, result.Email);
            Assert.NotNull(result.KisToken);
            Assert.Equal("test_access_token", result.KisToken.AccessToken);
            Assert.Equal("Bearer", result.KisToken.TokenType);
        }

        [Fact]
        public async Task GetByEmailAsync_NonExistentUser_ThrowsException()
        {
            var nonExistentEmail = "non_existent@example.com";

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repository.GetByEmailAsync(nonExistentEmail));
        }
    }
}