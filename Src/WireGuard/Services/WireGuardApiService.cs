using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WireGuard.Models;
using WireGuard.Models.Request;
using WireGuard.Services.Interface;

namespace WireGuard.Services;

public class WireGuardApiService : WireGuardApiBaseService, IWireGuardApiService
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public WireGuardApiService(HttpClient httpClient, IOptions<WireguardApiOptions> options)
        : base(httpClient, options)
    {
    }

    public async Task<WireguardClients[]?> GetWireguardClients(CancellationToken cancellationToken)
    {
        using var responseMessage = await SendWithAuthRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get, "/api/wireguard/client"),
            cancellationToken);

        var response = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<WireguardClients[]>(response, _jsonSerializerOptions);
    }

    public async Task<byte[]> DownloadConfigAsync(Guid id, CancellationToken cancellationToken)
    {
        using var responseMessage = await SendWithAuthRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"/api/wireguard/client/{id}/configuration"),
            cancellationToken);

        return await responseMessage.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task DeleteClientAsync(Guid id, CancellationToken cancellationToken)
    {
        using var responseMessage = await SendWithAuthRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Delete, $"/api/wireguard/client/{id}"),
            cancellationToken);
    }

    public async Task AddClientAsync(string name, CancellationToken cancellationToken)
    {
        var authRequest = new AddClientRequest
        {
            Name = name,
        };

        using var responseMessage = await SendWithAuthRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "/api/wireguard/client")
            {
                Content = new StringContent(JsonSerializer.Serialize(authRequest), Encoding.UTF8, "application/json"),
            },
            cancellationToken);
    }
}