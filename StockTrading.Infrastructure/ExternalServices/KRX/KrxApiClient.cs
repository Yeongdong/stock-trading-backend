using System.Net.Http.Json;
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
    }

    public async Task<KrxStockListResponse> GetStockListAsync()
    {
        _logger.LogInformation("KRX API 상장법인목록 조회 시작");

        var requestData = CreateStockListRequest();
        var content = new FormUrlEncodedContent(requestData);

        var response = await _httpClient.PostAsync(_settings.StockListEndpoint, content);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadFromJsonAsync<KrxStockListResponse>();

        if (jsonResponse?.Stocks == null)
            throw new InvalidOperationException("KRX API 응답이 올바르지 않습니다.");

        _logger.LogInformation("KRX API 응답 수신: {Count}개 종목", jsonResponse.Stocks.Count);
        return jsonResponse;
    }

    private Dictionary<string, string> CreateStockListRequest()
    {
        return new Dictionary<string, string>
        {
            ["bld"] = _settings.BuildId,
            ["mktId"] = "ALL",
            ["trdDd"] = DateTime.Now.ToString("yyyyMMdd"),
            ["money"] = "1",
            ["csvxls_isNo"] = "false"
        };
    }
}