using System.Security.Claims;
using StockTrading.DataAccess.DTOs;

namespace StockTrading.Tests.Integration.Interfaces;

/// <summary>
/// 통합테스트용 인증 관리 인터페이스
/// JWT 토큰 생성 및 인증 시뮬레이션을 담당
/// </summary>
public interface IAuthenticationHelper
{
    /// <summary>
    /// 기본 테스트 사용자로 JWT 토큰 생성
    /// </summary>
    string GenerateTestToken();

    /// <summary>
    /// 특정 사용자로 JWT 토큰 생성
    /// </summary>
    string GenerateTokenForUser(UserDto user);

    /// <summary>
    /// 커스텀 클레임으로 JWT 토큰 생성
    /// </summary>
    string GenerateTokenWithClaims(params Claim[] claims);

    /// <summary>
    /// 특정 역할(Role)로 JWT 토큰 생성
    /// </summary>
    string GenerateTokenWithRole(string role, string email = null, string name = null);

    /// <summary>
    /// 만료된 JWT 토큰 생성 (만료 테스트용)
    /// </summary>
    string GenerateExpiredToken();

    /// <summary>
    /// 잘못된 서명의 JWT 토큰 생성 (보안 테스트용)
    /// </summary>
    string GenerateInvalidSignatureToken();

    /// <summary>
    /// 인증된 HTTP 클라이언트 생성
    /// Authorization 헤더가 설정된 클라이언트 반환
    /// </summary>
    HttpClient CreateAuthenticatedClient();

    /// <summary>
    /// 특정 사용자로 인증된 HTTP 클라이언트 생성
    /// </summary>
    HttpClient CreateAuthenticatedClientForUser(UserDto user);

    /// <summary>
    /// 특정 토큰으로 인증된 HTTP 클라이언트 생성
    /// </summary>
    HttpClient CreateAuthenticatedClientWithToken(string token);

    /// <summary>
    /// Google OAuth 토큰 모킹
    /// Google 로그인 테스트용
    /// </summary>
    string GenerateMockGoogleCredential(string email, string name, string googleId);

    /// <summary>
    /// 토큰 검증 (테스트 검증용)
    /// </summary>
    ClaimsPrincipal ValidateToken(string token);

    /// <summary>
    /// 현재 설정된 기본 테스트 사용자 정보 조회
    /// </summary>
    UserDto GetDefaultTestUser();
}