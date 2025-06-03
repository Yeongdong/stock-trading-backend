using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.External.KRX.Responses;
using StockTrading.Domain.Settings;

public class KrxApiClient
{
    private readonly HttpClient _httpClient;
    private readonly KrxApiSettings _settings;
    private readonly ILogger<KrxApiClient> _logger;

    public KrxApiClient(HttpClient httpClient, IOptions<KrxApiSettings> settings, ILogger<KrxApiClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        ConfigureHttpClient();
    }

    public async Task<KrxStockListResponse> GetStockListAsync()
    {
        _logger.LogInformation("🔄 [KRX] 주식종목 목록 조회 시작");

        var retryCount = 0;
        Exception lastException = null!;

        while (retryCount <= _settings.RetryCount)
        {
            try
            {
                var requestData = CreateStockListRequest();
                _logger.LogDebug("📤 [KRX] API 요청: {Endpoint}", _settings.StockListEndpoint);

                var response = await _httpClient.PostAsync(_settings.StockListEndpoint, requestData);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await ReadResponseContentAsync(response);
                    throw new HttpRequestException($"KRX API 호출 실패: {response.StatusCode} - {errorContent}");
                }

                var jsonContent = await ReadResponseContentAsync(response);
                _logger.LogDebug("📥 [KRX] API 응답 수신: {Length} bytes", jsonContent.Length);

                var result = ParseStockListResponse(jsonContent);
                _logger.LogInformation("✅ [KRX] 주식종목 목록 조회 완료: {Count}개 종목", result.Stocks.Count);
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;
                if (retryCount <= _settings.RetryCount)
                {
                    _logger.LogWarning("⚠️ [KRX] API 호출 실패 ({Retry}/{MaxRetry}): {Error}",
                        retryCount, _settings.RetryCount, ex.Message);
                    await Task.Delay(_settings.RetryDelayMs * retryCount);
                }
                else
                {
                    _logger.LogError(ex, "❌ [KRX] API 호출 최대 재시도 초과");
                }
            }
        }

        throw new InvalidOperationException("KRX API 호출 실패: 최대 재시도 횟수 초과", lastException);
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_settings.UserAgent);
        _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://data.krx.co.kr/");
        _httpClient.DefaultRequestHeaders.Add("Origin", "https://data.krx.co.kr");
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ko-KR,ko;q=0.9,en-US;q=0.8,en;q=0.7");
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        _logger.LogDebug("🔧 [KRX] HttpClient 설정 완료: BaseAddress={Base}, Timeout={Timeout}s",
            _httpClient.BaseAddress, _settings.TimeoutSeconds);
    }

    private async Task<string> ReadResponseContentAsync(HttpResponseMessage response)
    {
        var contentBytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

        if (contentType.Contains("euc-kr", StringComparison.OrdinalIgnoreCase))
        {
            return Encoding.GetEncoding("EUC-KR").GetString(contentBytes);
        }

        // 보통 KRX는 EUC-KR을 쓰지만, 혹시 UTF-8인 경우를 위해 시도
        try
        {
            return Encoding.UTF8.GetString(contentBytes);
        }
        catch
        {
            return Encoding.GetEncoding("EUC-KR").GetString(contentBytes);
        }
    }

    private FormUrlEncodedContent CreateStockListRequest()
    {
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("bld", _settings.StockListBuildId),
            new KeyValuePair<string, string>("mktId", "ALL"),
            new KeyValuePair<string, string>("share", "1"),
            new KeyValuePair<string, string>("csvxls_isNo", "false")
        });

        _logger.LogDebug("📝 [KRX] 요청 데이터 생성: BuildId={BuildId}, Market=ALL", _settings.StockListBuildId);
        return formData;
    }

    private KrxStockListResponse ParseStockListResponse(string jsonContent)
    {
        _logger.LogDebug("📥 [KRX] 원본 JSON 일부: {Snippet}", jsonContent.Substring(0, Math.Min(jsonContent.Length, 500)));

        using var jsonDoc = JsonDocument.Parse(jsonContent);
        var root = jsonDoc.RootElement;

        if (!root.TryGetProperty("OutBlock_1", out var dataArray))
            throw new InvalidOperationException("KRX API 응답에서 'OutBlock_1' 속성을 찾을 수 없습니다.");

        var stocks = new List<KrxStockItem>();
        var invalidCount = 0;

        foreach (var item in dataArray.EnumerateArray())
        {
            try
            {
                var stock = JsonSerializer.Deserialize<KrxStockItem>(item.GetRawText());
                if (stock?.IsValid() == true)
                    stocks.Add(stock);
                else
                    invalidCount++;
            }
            catch (JsonException je)
            {
                _logger.LogWarning("⚠️ 역직렬화 실패: {Message}", je.Message);

                invalidCount++;
            }
        }

        _logger.LogInformation("📊 [KRX] 데이터 파싱 완료: 유효={Valid}개, 무효={Invalid}개",
            stocks.Count, invalidCount);

        return new KrxStockListResponse { Stocks = stocks };
    }
}