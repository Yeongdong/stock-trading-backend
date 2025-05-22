namespace StockTrading.Tests.Integration;

/// <summary>
/// 통합테스트용 상수 정의
/// </summary>
public static class IntegrationTestConstants
{
    /// <summary>
    /// 테스트 데이터베이스 이름 접두사
    /// </summary>
    public const string TestDatabasePrefix = "IntegrationTest_";
    
    /// <summary>
    /// 테스트용 설정 파일명
    /// </summary>
    public const string TestConfigFileName = "appsettings.Testing.json";
    
    /// <summary>
    /// 테스트 환경명
    /// </summary>
    public const string TestEnvironmentName = "Testing";
    
    /// <summary>
    /// KIS API 호스트 식별자
    /// </summary>
    public const string KisApiHostIdentifier = "koreainvestment";
    
    /// <summary>
    /// 기본 성공 응답 메시지
    /// </summary>
    public const string DefaultSuccessMessage = "{\"message\":\"success\"}";
}