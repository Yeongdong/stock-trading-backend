using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.DTOs.External.KRX.Responses;
using StockTrading.Domain.Settings;

namespace StockTrading.Infrastructure.ExternalServices.KRX;

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
        _logger.LogInformation("KRX 주식종목 목록 조회 시작");

        var retryCount = 0;
        Exception lastException = null!;

        while (retryCount <= _settings.RetryCount)
        {
            var requestData = CreateStockListRequest();
            var response = await _httpClient.PostAsync(_settings.StockListEndpoint, requestData);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await ReadResponseContentAsync(response);
                throw new HttpRequestException($"KRX API 호출 실패: {response.StatusCode} - {errorContent}");
            }

            var jsonContent = await ReadResponseContentAsync(response);
            var result = ParseStockListResponse(jsonContent);

            _logger.LogInformation("KRX 주식종목 목록 조회 완료: {Count}개 종목", result.Stocks.Count);
            return result;
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
    }

    private async Task<string> ReadResponseContentAsync(HttpResponseMessage response)
    {
        var contentBytes = await response.Content.ReadAsByteArrayAsync();
        return Encoding.GetEncoding("EUC-KR").GetString(contentBytes);
    }

    private FormUrlEncodedContent CreateStockListRequest()
    {
        return new FormUrlEncodedContent([
            new KeyValuePair<string, string>("bld", _settings.StockListBuildId),
            new KeyValuePair<string, string>("mktId", "ALL"),
            new KeyValuePair<string, string>("share", "1"),
            new KeyValuePair<string, string>("csvxls_isNo", "false")
        ]);
    }

    private KrxStockListResponse ParseStockListResponse(string jsonContent)
    {
        using var jsonDoc = JsonDocument.Parse(jsonContent);
        var root = jsonDoc.RootElement;

        if (!root.TryGetProperty("OutBlock_1", out var dataArray))
            throw new InvalidOperationException("KRX API 응답에서 'OutBlock_1' 속성을 찾을 수 없습니다.");

        var stocks = new List<KrxStockItem>();
        var invalidCount = 0;

        foreach (var stock in dataArray.EnumerateArray()
                     .Select(item => JsonSerializer.Deserialize<KrxStockItem>(item.GetRawText())))
        {
            if (stock?.IsValid() == true)
                stocks.Add(stock);
            else
                invalidCount++;
        }

        if (invalidCount > 0)
            _logger.LogWarning("유효하지 않은 종목 데이터: {InvalidCount}개", invalidCount);

        return new KrxStockListResponse { Stocks = stocks };
    }
}