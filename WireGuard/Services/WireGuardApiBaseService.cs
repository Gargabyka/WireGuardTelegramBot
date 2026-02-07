using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WireGuardTelegramBot.WireGuard.Models;
using WireGuardTelegramBot.WireGuard.Models.Request;

namespace WireGuardTelegramBot.WireGuard.Services;

public abstract class WireGuardApiBaseService
{
    private readonly HttpClient _httpClient;

    private readonly WireguardApiOptions _wireguardApiOptions;
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private volatile bool _isAuthenticated;

    protected WireGuardApiBaseService(IOptions<WireguardApiOptions> wireGuardOptions)
    {
        _wireguardApiOptions = wireGuardOptions.Value;

        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_wireguardApiOptions.ApiUrl),
        };
    }

    protected async Task<HttpResponseMessage> SendWithAuthRetryAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        var response = await _httpClient.SendAsync(requestFactory(), cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            response.Dispose();

            await EnsureAuthenticatedAsync(cancellationToken, force: true);

            response = await _httpClient.SendAsync(requestFactory(), cancellationToken);
        }

        response.EnsureSuccessStatusCode();
        return response;
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken, bool force = false)
    {
        if (!force && _isAuthenticated)
        {
            return;
        }

        await _authLock.WaitAsync(cancellationToken);
        try
        {
            if (!force && _isAuthenticated)
            {
                return;
            }

            await WireguardAuthAsync(cancellationToken);
            _isAuthenticated = true;
        }
        finally
        {
            _authLock.Release();
        }
    }

    private async Task WireguardAuthAsync(CancellationToken cancellationToken)
    {
        var authRequest = new AuthorizationRequest
        {
            Password = _wireguardApiOptions.ApiPassword,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/session");
        request.Content = new StringContent(JsonSerializer.Serialize(authRequest), Encoding.UTF8, "application/json");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}