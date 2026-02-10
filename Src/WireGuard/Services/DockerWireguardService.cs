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
		// Без process substitution: делаем temp file внутри контейнера
		var cmd = $"wg-quick strip {_wg.InterfaceName} > /tmp/stripped.conf && wg syncconf {_wg.InterfaceName} /tmp/stripped.conf";
		return ExecInTargetContainerAsync(["sh", "-lc", cmd], cancellationToken);
	}

	public Task ApplyConfigWithDownUpAsync(CancellationToken cancellationToken)
	{
		var cmd = $"wg-quick down {_wg.InterfaceName} || true; wg-quick up {_wg.InterfaceName}";
		return ExecInTargetContainerAsync(["sh", "-lc", cmd], cancellationToken);
	}

	private async Task ExecInTargetContainerAsync(string[] cmd, CancellationToken cancellationToken)
	{
		if (!_docker.Enabled)
		{
			throw new InvalidOperationException("WireGuardDocker.Enabled=false, но используется DockerWireguardService.");
		}

		using var http = CreateDockerHttpClient(_docker.DockerSocketPath);

		// 1) Найдём контейнер по имени
		var containerId = await FindContainerIdByNameAsync(http, _docker.TargetContainerName, cancellationToken);
		if (containerId is null)
		{
			throw new InvalidOperationException($"Не найден контейнер '{_docker.TargetContainerName}'. Проверьте container_name в docker-compose.");
		}

		// 2) Создаём exec
		var execCreate = new
		{
			AttachStdout = false,
			AttachStderr = false,
			Tty = false,
			Cmd = cmd,
		};

		var execCreateResp = await http.PostAsJsonAsync($"containers/{containerId}/exec", execCreate, cancellationToken);
		execCreateResp.EnsureSuccessStatusCode();

		var execCreateJson = await execCreateResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
		var execId = execCreateJson.GetProperty("Id").GetString();
		if (string.IsNullOrWhiteSpace(execId))
		{
			throw new InvalidOperationException("Docker exec: empty exec id.");
		}

		// 3) Запускаем exec (detach) и ждём завершения через inspect
		var execStart = new { Detach = true, Tty = false };
		var execStartResp = await http.PostAsJsonAsync($"exec/{execId}/start", execStart, cancellationToken);
		execStartResp.EnsureSuccessStatusCode();

		// 4) Poll inspect до завершения
		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var inspect = await http.GetFromJsonAsync<JsonElement>($"exec/{execId}/json", cancellationToken);
			var running = inspect.GetProperty("Running").GetBoolean();
			if (!running)
			{
				var exitCode = inspect.TryGetProperty("ExitCode", out var ec) ? ec.GetInt32() : -1;
				if (exitCode != 0)
				{
					throw new InvalidOperationException($"Команда WireGuard внутри '{_docker.TargetContainerName}' завершилась с кодом {exitCode}.");
				}

				return;
			}

			await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
		}
	}

	private async Task<string?> FindContainerIdByNameAsync(HttpClient http, string name, CancellationToken cancellationToken)
	{
		// Docker API: /containers/json возвращает список с Names: ["/wg-easy"]
		var containers = await http.GetFromJsonAsync<JsonElement>("containers/json?all=1", cancellationToken);
		foreach (var c in containers.EnumerateArray())
		{
			if (!c.TryGetProperty("Names", out var names))
			{
				continue;
			}

			foreach (var n in names.EnumerateArray())
			{
				var s = n.GetString();
				if (string.Equals(s, "/" + name, StringComparison.Ordinal))
				{
					return c.GetProperty("Id").GetString();
				}
			}
		}

		return null;
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
			BaseAddress = new Uri("http://localhost/v1.41/"),
		};
	}
}