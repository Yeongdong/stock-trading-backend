using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Domain.Settings.ExternalServices;

namespace StockTrading.Infrastructure.ExternalServices.Finnhub.Common;

public abstract class FinnhubApiClientBase
{
    protected readonly HttpClient _httpClient;
    protected readonly FinnhubSettings _settings;
    protected readonly ILogger _logger;

    protected FinnhubApiClientBase(HttpClient httpClient, IOptions<FinnhubSettings> settings, ILogger logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    protected void SetStandardHeaders(HttpRequestMessage request)
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            _logger.LogError("Finnhub API 키가 설정되지 않았습니다.");
            throw new Exception("Finnhub API 키가 필요합니다.");
        }
        request.Headers.Add("X-Finnhub-Token", _settings.ApiKey);
    }

    protected string BuildGetUrl(string path, Dictionary<string, string> queryParams)
    {
        var query = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
        return $"{path}?{query}";
    }

    protected async Task<string> ValidateAndReadResponse(HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Finnhub API 호출 실패 ({response.StatusCode}): {responseContent}");

        return responseContent;
    }
}