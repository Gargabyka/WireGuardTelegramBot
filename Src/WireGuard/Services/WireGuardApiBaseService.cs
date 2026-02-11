using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WireGuard.Models;
using WireGuard.Models.Options;
using WireGuard.Models.Request;

namespace WireGuard.Services;

public abstract class WireGuardApiBaseService : IDisposable
{
    private readonly HttpClient _httpClient;

    private readonly WireguardApiOptions _wireguardApiOptions;
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private volatile bool _isAuthenticated;
    private bool _disposed;

    protected WireGuardApiBaseService(
        HttpClient httpClient,
        IOptions<WireguardApiOptions> wireGuardOptions)
    {
        _httpClient = httpClient;
        _wireguardApiOptions = wireGuardOptions.Value;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Отправляет запрос с авторизацией и повторной попыткой при необходимости
    /// </summary>
    /// <param name="requestFactory">Запрос на отправку</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Ответ на запрос</returns>
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

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Важно: HttpClient НЕ освобождаем — им управляет IHttpClientFactory
            _authLock.Dispose();
        }

        _disposed = true;
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