using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockTrading.Application.Features.Users.DTOs;
using StockTrading.Domain.Settings.ExternalServices;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.Common;

public abstract class KisApiClientBase
{
    protected readonly HttpClient _httpClient;
    protected readonly KoreaInvestmentSettings _settings;
    protected readonly ILogger _logger;

    protected KisApiClientBase(HttpClient httpClient, IOptions<KoreaInvestmentSettings> settings, ILogger logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    protected void SetStandardHeaders(HttpRequestMessage request, string transactionId, UserInfo user)
    {
        request.Headers.Add("Authorization", $"Bearer {user.KisToken?.AccessToken}");
        request.Headers.Add("appkey", user.KisAppKey);
        request.Headers.Add("appsecret", user.KisAppSecret);
        request.Headers.Add("tr_id", transactionId);
        request.Headers.Add("custtype", "P");
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
            throw new Exception($"KIS API 호출 실패 ({response.StatusCode}): {responseContent}");

        return responseContent;
    }
}