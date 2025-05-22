using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using StockTrading.Tests.Integration.Interfaces;

namespace StockTrading.Tests.Integration.Implementations;

/// <summary>
/// 통합테스트용 HTTP 클라이언트 관리 구현체
/// HTTP 요청/응답 처리 및 클라이언트 설정을 담당
/// </summary>
public class HttpClientHelper : IHttpClientHelper
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ILogger<HttpClientHelper> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _requestLoggingEnabled;
    private TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);

    public HttpClientHelper(WebApplicationFactory<Program> factory, ILogger<HttpClientHelper> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    /// <summary>
    /// 기본 HTTP 클라이언트 생성
    /// </summary>
    public HttpClient CreateClient()
    {
        var client = _factory.CreateClient();
        ConfigureClient(client);
        return client;
    }

    /// <summary>
    /// 특정 기본 주소로 HTTP 클라이언트 생성
    /// </summary>
    public HttpClient CreateClient(string baseAddress)
    {
        var client = _factory.CreateClient();
        client.BaseAddress = new Uri(baseAddress);
        ConfigureClient(client);
        return client;
    }

    /// <summary>
    /// 커스텀 헤더가 설정된 HTTP 클라이언트 생성
    /// </summary>
    public HttpClient CreateClientWithHeaders(Dictionary<string, string> headers)
    {
        var client = CreateClient();

        foreach (var header in headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        LogClientCreation("커스텀 헤더", headers.Count.ToString());
        return client;
    }

    /// <summary>
    /// JSON 요청을 위한 HTTP 클라이언트 생성
    /// </summary>
    public HttpClient CreateJsonClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        LogClientCreation("JSON", "application/json");
        return client;
    }

    /// <summary>
    /// 폼 데이터 요청을 위한 HTTP 클라이언트 생성
    /// </summary>
    public HttpClient CreateFormClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

        LogClientCreation("Form", "application/x-www-form-urlencoded");
        return client;
    }

    /// <summary>
    /// GET 요청 헬퍼
    /// </summary>
    public async Task<HttpResponseMessage> GetAsync(string requestUri, HttpClient client = null)
    {
        try
        {
            LogRequest("GET", requestUri);
            var response = await client.GetAsync(requestUri);
            LogResponse("GET", requestUri, response);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET 요청 실패: {RequestUri}", requestUri);
            throw;
        }
    }

    /// <summary>
    /// POST 요청 헬퍼 (JSON)
    /// </summary>
    public async Task<HttpResponseMessage> PostJsonAsync<T>(string requestUri, T content, HttpClient client = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(content, _jsonOptions);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            LogRequest("POST", requestUri, json);
            var response = await client.PostAsync(requestUri, stringContent);
            LogResponse("POST", requestUri, response);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST 요청 실패: {RequestUri}", requestUri);
            throw;
        }
    }

    /// <summary>
    /// PUT 요청 헬퍼 (JSON)
    /// </summary>
    public async Task<HttpResponseMessage> PutJsonAsync<T>(string requestUri, T content, HttpClient client = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(content, _jsonOptions);
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            LogRequest("PUT", requestUri, json);
            var response = await client.PutAsync(requestUri, stringContent);
            LogResponse("PUT", requestUri, response);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PUT 요청 실패: {RequestUri}", requestUri);
            throw;
        }
    }

    /// <summary>
    /// DELETE 요청 헬퍼
    /// </summary>
    public async Task<HttpResponseMessage> DeleteAsync(string requestUri, HttpClient client = null)
    {
        try
        {
            LogRequest("DELETE", requestUri);
            var response = await client.DeleteAsync(requestUri);
            LogResponse("DELETE", requestUri, response);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DELETE 요청 실패: {RequestUri}", requestUri);
            throw;
        }
    }

    /// <summary>
    /// 응답을 특정 타입으로 역직렬화
    /// </summary>
    public async Task<T> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("응답 내용이 비어있습니다");
                return default(T);
            }

            var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);

            if (_requestLoggingEnabled)
                _logger.LogDebug("응답 역직렬화 완료: {Type}", typeof(T).Name);

            return result;
        }
        catch (JsonException ex)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogError(ex, "JSON 역직렬화 실패. 응답 내용: {Content}", content);
            throw new InvalidOperationException($"응답을 {typeof(T).Name} 타입으로 역직렬화할 수 없습니다.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "응답 역직렬화 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 응답 상태 코드 확인 및 예외 처리
    /// </summary>
    public async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var content = await response.Content.ReadAsStringAsync();
        var errorMessage = $"HTTP 요청 실패: {response.StatusCode} - {response.ReasonPhrase}";

        if (!string.IsNullOrWhiteSpace(content))
            errorMessage += $"\n응답 내용: {content}";

        _logger.LogError("HTTP 요청 실패: {StatusCode} {ReasonPhrase}\n응답 내용: {Content}",
            response.StatusCode, response.ReasonPhrase, content);

        throw new HttpRequestException(errorMessage);
    }

    /// <summary>
    /// 응답 내용을 문자열로 읽기
    /// </summary>
    public async Task<string> ReadResponseContentAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();

            if (_requestLoggingEnabled)
                _logger.LogDebug("응답 내용 읽기 완료: {Length}자", content.Length);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "응답 내용 읽기 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 요청 로깅 활성화/비활성화
    /// </summary>
    public void EnableRequestLogging(bool enable = true)
    {
        _requestLoggingEnabled = enable;
        _logger.LogInformation("요청 로깅 {Status}", enable ? "활성화" : "비활성화");
    }

    /// <summary>
    /// 요청 타임아웃 설정
    /// </summary>
    public void SetTimeout(TimeSpan timeout)
    {
        _defaultTimeout = timeout;
        _logger.LogInformation("요청 타임아웃 설정: {Timeout}", timeout);
    }

    /// <summary>
    /// 클라이언트 기본 설정
    /// </summary>
    private void ConfigureClient(HttpClient client)
    {
        client.Timeout = _defaultTimeout;

        client.DefaultRequestHeaders.Add("User-Agent", "IntegrationTest/1.0");

        if (_requestLoggingEnabled)
            _logger.LogDebug("HTTP 클라이언트 생성 완료");
    }

    /// <summary>
    /// 클라이언트 생성 로깅
    /// </summary>
    private void LogClientCreation(string clientType, string detail)
    {
        if (_requestLoggingEnabled)
            _logger.LogDebug("{ClientType} HTTP 클라이언트 생성: {Detail}", clientType, detail);
    }

    /// <summary>
    /// 요청 로깅
    /// </summary>
    private void LogRequest(string method, string uri, string body = null)
    {
        if (!_requestLoggingEnabled) return;

        if (string.IsNullOrEmpty(body))
            _logger.LogDebug("HTTP 요청: {Method} {Uri}", method, uri);
        else
            _logger.LogDebug("HTTP 요청: {Method} {Uri}\n요청 본문: {Body}", method, uri, body);
    }

    /// <summary>
    /// 응답 로깅
    /// </summary>
    private void LogResponse(string method, string uri, HttpResponseMessage response)
    {
        if (!_requestLoggingEnabled) return;

        _logger.LogDebug("HTTP 응답: {Method} {Uri} - {StatusCode} {ReasonPhrase}",
            method, uri, response.StatusCode, response.ReasonPhrase);
    }

    /// <summary>
    /// 편의 메서드: JSON 응답 GET 요청
    /// </summary>
    public async Task<T> GetJsonAsync<T>(string requestUri, HttpClient client = null)
    {
        var response = await GetAsync(requestUri, client);
        await EnsureSuccessStatusCodeAsync(response);
        return await DeserializeResponseAsync<T>(response);
    }

    /// <summary>
    /// 편의 메서드: JSON 요청/응답 POST 요청
    /// </summary>
    public async Task<TResponse> PostJsonAsync<TRequest, TResponse>(
        string requestUri,
        TRequest content,
        HttpClient client = null)
    {
        var response = await PostJsonAsync(requestUri, content, client);
        await EnsureSuccessStatusCodeAsync(response);
        return await DeserializeResponseAsync<TResponse>(response);
    }

    /// <summary>
    /// 편의 메서드: JSON 요청/응답 PUT 요청
    /// </summary>
    public async Task<TResponse> PutJsonAsync<TRequest, TResponse>(
        string requestUri,
        TRequest content,
        HttpClient client = null)
    {
        var response = await PutJsonAsync(requestUri, content, client);
        await EnsureSuccessStatusCodeAsync(response);
        return await DeserializeResponseAsync<TResponse>(response);
    }

    /// <summary>
    /// 편의 메서드: 성공 상태 코드 확인이 포함된 DELETE 요청
    /// </summary>
    public async Task DeleteWithSuccessCheckAsync(string requestUri, HttpClient client = null)
    {
        var response = await DeleteAsync(requestUri, client);
        await EnsureSuccessStatusCodeAsync(response);
    }
}