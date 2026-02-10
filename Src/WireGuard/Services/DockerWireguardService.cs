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
	{
		// Мягкое применение на ХОСТЕ без перезапуска интерфейса.
		// Берём strip (он читает /etc/wireguard/wg0.conf на хосте) → temp file → wg syncconf.
		var cmd =
			$"tmp=\"$(mktemp /tmp/{_wg.InterfaceName}.XXXXXX.conf)\" && " +
			$"wg-quick strip {_wg.InterfaceName} > \"$tmp\" && " +
			$"wg syncconf {_wg.InterfaceName} \"$tmp\" && " +
			$"rm -f \"$tmp\"";

		return RunHostHelperAsync(["sh", "-lc", cmd], cancellationToken);
	}

	public Task ApplyConfigWithDownUpAsync(CancellationToken cancellationToken)
	{
		// Жёсткое применение на ХОСТЕ (как вы делаете вручную по SSH).
		var cmd = $"wg-quick down {_wg.InterfaceName} || true; wg-quick up {_wg.InterfaceName}";
		return RunHostHelperAsync(["sh", "-lc", cmd], cancellationToken);
	}

	private static async Task EnsureImageAsync(HttpClient http, string image, CancellationToken cancellationToken)
	{
		var pullUrl = $"images/create?fromImage={Uri.EscapeDataString(image)}";
		var resp = await http.PostAsync(pullUrl, content: null, cancellationToken);
		resp.EnsureSuccessStatusCode();
	}

	private static HttpClient CreateDockerHttpClient(string socketPath)
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
			BaseAddress = new Uri("http://localhost/v1.41/"),
		};
	}

	private async Task RunHostHelperAsync(string[] cmd, CancellationToken cancellationToken)
	{
		if (!_docker.Enabled)
		{
			throw new InvalidOperationException("WireGuardDocker.Enabled=false, но используется DockerWireguardService.");
		}

		using var http = CreateDockerHttpClient(_docker.DockerSocketPath);

		await EnsureImageAsync(http, _docker.HelperImage, cancellationToken);

		var helperName = $"wg-host-run-{Guid.NewGuid():N}";
		var createBody = new
		{
			Image = _docker.HelperImage,
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
			$"containers/create?name={Uri.EscapeDataString(helperName)}",
			createBody,
			cancellationToken);

		createResp.EnsureSuccessStatusCode();

		var createJson = await createResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
		var id = createJson.GetProperty("Id").GetString();
		if (string.IsNullOrWhiteSpace(id))
		{
			throw new InvalidOperationException("Docker helper: container id is empty.");
		}

		try
		{
			var startResp = await http.PostAsync($"containers/{id}/start", content: null, cancellationToken);
			startResp.EnsureSuccessStatusCode();

			var waitResp = await http.PostAsync($"containers/{id}/wait", content: null, cancellationToken);
			waitResp.EnsureSuccessStatusCode();

			var waitJson = await waitResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
			var statusCode = waitJson.GetProperty("StatusCode").GetInt32();

			var logs = await http.GetStringAsync($"containers/{id}/logs?stdout=1&stderr=1", cancellationToken);

			if (statusCode != 0)
			{
				throw new InvalidOperationException($"Host WireGuard apply failed (exit={statusCode}). Logs:\n{logs}");
			}
		}
		finally
		{
			try
            {
                await http.DeleteAsync($"containers/{id}?force=true&v=true", cancellationToken);
            }
            catch
            { /* ignore */
            }
		}
	}
}