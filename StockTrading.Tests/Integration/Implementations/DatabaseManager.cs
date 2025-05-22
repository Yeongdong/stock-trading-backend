using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockTrading.Infrastructure.Repositories;
using StockTrading.Tests.Integration.Interfaces;
using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Integration.Implementations;

/// <summary>
/// 통합테스트용 데이터베이스 관리 구현체
/// 데이터베이스 생명주기와 데이터 관리를 담당
/// </summary>
public class DatabaseManager : IDatabaseManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TestDataFactory _testDataFactory;
    private readonly ILogger<DatabaseManager> _logger;
    private User _cachedTestUser;

    public DatabaseManager(
        IServiceProvider serviceProvider,
        TestDataFactory testDataFactory,
        ILogger<DatabaseManager> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _testDataFactory = testDataFactory ?? throw new ArgumentNullException(nameof(testDataFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 데이터베이스 초기화
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("데이터베이스 초기화 시작");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.EnsureCreatedAsync();

            _logger.LogInformation("데이터베이스 스키마 생성 완료");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "데이터베이스 초기화 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 테스트용 시드 데이터 생성
    /// </summary>
    public async Task SeedTestDataAsync()
    {
        try
        {
            _logger.LogInformation("테스트 데이터 시딩 시작");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 이미 데이터가 있으면 스킵
            if (await context.Users.AnyAsync())
            {
                _logger.LogInformation("테스트 데이터가 이미 존재함, 시딩 스킵");
                return;
            }

            var testUser = _testDataFactory.CreateTestUser();
            context.Users.Add(testUser);
            await context.SaveChangesAsync();

            var kisToken = _testDataFactory.CreateTestKisToken(testUser.Id);
            context.KisTokens.Add(kisToken);
            await context.SaveChangesAsync();

            _cachedTestUser = testUser;

            _logger.LogInformation("테스트 데이터 시딩 완료: 사용자 ID {UserId}", testUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "테스트 데이터 시딩 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 특정 테스트를 위한 커스텀 데이터 시딩
    /// </summary>
    public async Task SeedCustomDataAsync<T>(params T[] entities) where T : class
    {
        if (entities == null || entities.Length == 0)
        {
            _logger.LogWarning("시딩할 엔티티가 없습니다");
            return;
        }

        try
        {
            _logger.LogInformation("{EntityType} 엔티티 {Count}개 시딩 시작", typeof(T).Name, entities.Length);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            foreach (var entity in entities)
            {
                context.Set<T>().Add(entity);
            }

            var savedCount = await context.SaveChangesAsync();
            _logger.LogInformation("{EntityType} 엔티티 {SavedCount}개 시딩 완료", typeof(T).Name, savedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{EntityType} 엔티티 시딩 중 오류 발생", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// 데이터베이스 전체 정리
    /// </summary>
    public async Task CleanupAsync()
    {
        try
        {
            _logger.LogInformation("데이터베이스 전체 정리 시작");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await CleanupTableAsync<KisToken>(context, "KIS 토큰");
            await CleanupTableAsync<User>(context, "사용자");

            _cachedTestUser = null;

            _logger.LogInformation("데이터베이스 전체 정리 완료");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "데이터베이스 정리 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 특정 엔티티 타입만 정리
    /// </summary>
    public async Task CleanupAsync<T>() where T : class
    {
        try
        {
            _logger.LogInformation("{EntityType} 엔티티 정리 시작", typeof(T).Name);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await CleanupTableAsync<T>(context, typeof(T).Name);

            if (typeof(T) == typeof(User))
            {
                _cachedTestUser = null;
            }

            _logger.LogInformation("{EntityType} 엔티티 정리 완료", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{EntityType} 엔티티 정리 중 오류 발생", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// 트랜잭션 시작
    /// </summary>
    public async Task<IDisposable> BeginTransactionAsync()
    {
        _logger.LogDebug("데이터베이스 트랜잭션 시작");

        var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var transaction = await context.Database.BeginTransactionAsync();

        return new TransactionScope(transaction, scope, _logger);
    }

    /// <summary>
    /// 데이터베이스 상태 확인
    /// </summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // 간단한 쿼리로 연결 상태 확인
            await context.Database.ExecuteSqlRawAsync("SELECT 1");

            _logger.LogDebug("데이터베이스 상태: 정상");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "데이터베이스 상태 확인 실패");
            return false;
        }
    }

    /// <summary>
    /// 특정 엔티티 개수 조회
    /// </summary>
    public async Task<int> CountAsync<T>() where T : class
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var count = await context.Set<T>().CountAsync();
            _logger.LogDebug("{EntityType} 엔티티 개수: {Count}", typeof(T).Name, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{EntityType} 엔티티 개수 조회 중 오류 발생", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// 테스트용 사용자 조회
    /// </summary>
    public async Task<User> GetTestUserAsync()
    {
        // 캐시된 사용자가 있으면 반환
        if (_cachedTestUser != null)
        {
            _logger.LogDebug("캐시된 테스트 사용자 반환: {UserId}", _cachedTestUser.Id);
            return _cachedTestUser;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var testUser = await context.Users
                .Include(u => u.KisToken)
                .FirstOrDefaultAsync();

            if (testUser == null)
            {
                _logger.LogWarning("테스트 사용자가 없습니다. 시드 데이터를 먼저 생성하세요.");
                throw new InvalidOperationException("테스트 사용자가 없습니다. SeedTestDataAsync()를 먼저 호출하세요.");
            }

            _cachedTestUser = testUser;

            _logger.LogDebug("데이터베이스에서 테스트 사용자 조회: {UserId}", testUser.Id);
            return testUser;
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "테스트 사용자 조회 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 테이블 정리 헬퍼 메서드
    /// </summary>
    private async Task CleanupTableAsync<T>(ApplicationDbContext context, string tableName) where T : class
    {
        var entities = await context.Set<T>().ToListAsync();
        if (entities.Any())
        {
            context.Set<T>().RemoveRange(entities);
            var deletedCount = await context.SaveChangesAsync();
            _logger.LogDebug("{TableName} 테이블에서 {DeletedCount}개 레코드 삭제", tableName, deletedCount);
        }
    }

    /// <summary>
    /// 트랜잭션 스코프 래퍼
    /// </summary>
    private class TransactionScope : IDisposable
    {
        private readonly Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction _transaction;
        private readonly IServiceScope _scope;
        private readonly ILogger _logger;
        private bool _disposed;

        public TransactionScope(
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
            IServiceScope scope,
            ILogger logger)
        {
            _transaction = transaction;
            _scope = scope;
            _logger = logger;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _transaction?.Dispose();
                    _scope?.Dispose();
                    _logger.LogDebug("데이터베이스 트랜잭션 종료");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "트랜잭션 종료 중 오류 발생");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}