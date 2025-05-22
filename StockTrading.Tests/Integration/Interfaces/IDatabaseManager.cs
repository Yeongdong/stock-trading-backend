using StockTradingBackend.DataAccess.Entities;

namespace StockTrading.Tests.Integration.Interfaces;

/// <summary>
/// 통합테스트용 데이터베이스 관리 인터페이스
/// 데이터베이스 생명주기와 데이터 관리를 담당
/// </summary>
public interface IDatabaseManager
{
    /// <summary>
    /// 데이터베이스 초기화
    /// 스키마 생성 및 기본 설정
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 테스트용 시드 데이터 생성
    /// </summary>
    Task SeedTestDataAsync();

    /// <summary>
    /// 특정 테스트를 위한 커스텀 데이터 시딩
    /// </summary>
    Task SeedCustomDataAsync<T>(params T[] entities) where T : class;

    /// <summary>
    /// 데이터베이스 전체 정리
    /// 모든 테이블 데이터 삭제
    /// </summary>
    Task CleanupAsync();

    /// <summary>
    /// 특정 엔티티 타입만 정리
    /// </summary>
    Task CleanupAsync<T>() where T : class;

    /// <summary>
    /// 트랜잭션 시작
    /// 테스트 격리를 위한 트랜잭션 관리
    /// </summary>
    Task<IDisposable> BeginTransactionAsync();

    /// <summary>
    /// 데이터베이스 상태 확인
    /// 연결 상태 및 스키마 존재 여부 확인
    /// </summary>
    Task<bool> IsHealthyAsync();

    /// <summary>
    /// 특정 엔티티 개수 조회
    /// 테스트 검증용
    /// </summary>
    Task<int> CountAsync<T>() where T : class;

    /// <summary>
    /// 테스트용 사용자 조회
    /// 시드 데이터로 생성된 기본 사용자 반환
    /// </summary>
    Task<User> GetTestUserAsync();
}