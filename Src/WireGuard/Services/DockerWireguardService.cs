using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WireGuard.Models;
using WireGuard.Services.Interface;

namespace WireGuard.Services;

public sealed class DockerWireguardService : IWireguardService
{
	private readonly WireGuardOptions _wg;
	private readonly DockerWireGuardRunnerOptions _docker;

	public DockerWireguardService(
		IOptions<WireGuardOptions> wireGuardOptions,
		IOptions<DockerWireGuardRunnerOptions> dockerOptions)
	{
		_wg = wireGuardOptions.Value;
		_docker = dockerOptions.Value;
	}

	public Task ApplyConfigAsync(CancellationToken cancellationToken)
		=> _wg.ApplyWithSyncConf
			? ApplyConfigWithSyncConfAsync(cancellationToken)
			: ApplyConfigWithDownUpAsync(cancellationToken);

	public Task ApplyConfigWithSyncConfAsync(CancellationToken cancellationToken)
		=> RunHelperAsync(["sh", "-lc", $"wg-quick strip {_wg.InterfaceName} | wg syncconf {_wg.InterfaceName} /dev/stdin"], cancellationToken);

	public Task ApplyConfigWithDownUpAsync(CancellationToken cancellationToken)
		=> RunHelperAsync(["sh", "-lc", $"wg-quick down {_wg.InterfaceName} || true && wg-quick up {_wg.InterfaceName}"], cancellationToken);

	private async Task RunHelperAsync(string[] cmd, CancellationToken cancellationToken)
	{
		if (!_docker.Enabled)
		{
			throw new InvalidOperationException("WireGuardDocker.Enabled=false, но используется DockerWireguardService.");
		}

		using var http = CreateDockerHttpClient(_docker.DockerSocketPath);

		// 1) ensure image (pull при необходимости)
		await EnsureImageAsync(http, _docker.Image, cancellationToken);

		// 2) create container
		var containerName = $"wg-helper-{Guid.NewGuid():N}";

		var createBody = new
		{
			_docker.Image,
			Cmd = cmd,
			HostConfig = new
			{
				NetworkMode = "host",
				CapAdd = new[] { "NET_ADMIN" },
				Binds = new[]
				{
					$"{_docker.HostWireGuardDir}:/etc/wireguard:rw",
				},
			},
		};

		var createResp = await http.PostAsJsonAsync(
			$"containers/create?name={Uri.EscapeDataString(containerName)}",
			createBody,
			cancellationToken);

		createResp.EnsureSuccessStatusCode();

		var createJson = await createResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
		var id = createJson.GetProperty("Id").GetString();
		if (string.IsNullOrWhiteSpace(id))
		{
			throw new InvalidOperationException("Docker: container id is empty.");
		}

		try
		{
			// 3) start
			var startResp = await http.PostAsync($"containers/{id}/start", content: null, cancellationToken);
			startResp.EnsureSuccessStatusCode();

			// 4) wait
			var waitResp = await http.PostAsync($"containers/{id}/wait", content: null, cancellationToken);
			waitResp.EnsureSuccessStatusCode();

			var waitJson = await waitResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
			var statusCode = waitJson.GetProperty("StatusCode").GetInt32();

			// 5) logs
			var logs = await http.GetStringAsync($"containers/{id}/logs?stdout=1&stderr=1", cancellationToken);

			if (statusCode != 0)
			{
				throw new InvalidOperationException($"WireGuard apply failed (exit={statusCode}). Logs: {logs}");
			}
		}
		finally
		{
			// remove container
			try
			{
				await http.DeleteAsync($"containers/{id}?force=true&v=true", cancellationToken);
			}
			catch
			{
				// ignore cleanup issues
			}
		}
	}

	private HttpClient CreateDockerHttpClient(string socketPath)
	{
		var handler = new SocketsHttpHandler
		{
			ConnectCallback = async (_, ct) =>
			{
				var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
				var endpoint = new UnixDomainSocketEndPoint(socketPath);
				await socket.ConnectAsync(endpoint, ct);
				return new NetworkStream(socket, ownsSocket: true);
			},
		};

		return new HttpClient(handler)
		{
			BaseAddress = new Uri("http://localhost/v1.41/"), // версия API Docker
		};
	}

	private async Task EnsureImageAsync(HttpClient http, string image, CancellationToken cancellationToken)
	{
		// Если образа нет — делаем pull. Для простоты: всегда дергаем pull; Docker сам закэширует.
		// image может быть "repo:tag"
		var pullUrl = $"images/create?fromImage={Uri.EscapeDataString(image)}";
		var resp = await http.PostAsync(pullUrl, content: null, cancellationToken);
		resp.EnsureSuccessStatusCode();
	}
}