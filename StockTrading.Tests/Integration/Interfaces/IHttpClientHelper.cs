namespace StockTrading.Tests.Integration.Interfaces;

/// <summary>
/// 통합테스트용 HTTP 클라이언트 관리 인터페이스
/// HTTP 요청/응답 처리 및 클라이언트 설정을 담당
/// </summary>
public interface IHttpClientHelper
{
    /// <summary>
    /// 기본 HTTP 클라이언트 생성
    /// </summary>
    HttpClient CreateClient();

    /// <summary>
    /// 특정 기본 주소로 HTTP 클라이언트 생성
    /// </summary>
    HttpClient CreateClient(string baseAddress);

    /// <summary>
    /// 커스텀 헤더가 설정된 HTTP 클라이언트 생성
    /// </summary>
    HttpClient CreateClientWithHeaders(Dictionary<string, string> headers);

    /// <summary>
    /// JSON 요청을 위한 HTTP 클라이언트 생성
    /// Content-Type: application/json 헤더 포함
    /// </summary>
    HttpClient CreateJsonClient();

    /// <summary>
    /// 폼 데이터 요청을 위한 HTTP 클라이언트 생성
    /// Content-Type: application/x-www-form-urlencoded 헤더 포함
    /// </summary>
    HttpClient CreateFormClient();

    /// <summary>
    /// GET 요청 헬퍼
    /// </summary>
    Task<HttpResponseMessage> GetAsync(string requestUri, HttpClient client = null);

    /// <summary>
    /// POST 요청 헬퍼 (JSON)
    /// </summary>
    Task<HttpResponseMessage> PostJsonAsync<T>(string requestUri, T content, HttpClient client = null);

    /// <summary>
    /// PUT 요청 헬퍼 (JSON)
    /// </summary>
    Task<HttpResponseMessage> PutJsonAsync<T>(string requestUri, T content, HttpClient client = null);

    /// <summary>
    /// DELETE 요청 헬퍼
    /// </summary>
    Task<HttpResponseMessage> DeleteAsync(string requestUri, HttpClient client = null);

    /// <summary>
    /// 응답을 특정 타입으로 역직렬화
    /// </summary>
    Task<T> DeserializeResponseAsync<T>(HttpResponseMessage response);

    /// <summary>
    /// 응답 상태 코드 확인 및 예외 처리
    /// </summary>
    Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response);

    /// <summary>
    /// 응답 내용을 문자열로 읽기
    /// </summary>
    Task<string> ReadResponseContentAsync(HttpResponseMessage response);

    /// <summary>
    /// 요청 로깅 활성화/비활성화
    /// 디버깅용
    /// </summary>
    void EnableRequestLogging(bool enable = true);

    /// <summary>
    /// 요청 타임아웃 설정
    /// </summary>
    void SetTimeout(TimeSpan timeout);
}