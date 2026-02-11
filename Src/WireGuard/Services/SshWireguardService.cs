using Microsoft.Extensions.Options;
using Renci.SshNet;
using WireGuard.Models.Options;
using WireGuard.Services.Interface;

namespace WireGuard.Services;

public sealed class SshWireguardService : IWireguardService
{
	private readonly WireGuardOptions _wg;
	private readonly WireGuardSshOptions _ssh;

	public SshWireguardService(
		IOptions<WireGuardOptions> wireGuardOptions,
		IOptions<WireGuardSshOptions> sshOptions)
	{
		_wg = wireGuardOptions.Value;
		_ssh = sshOptions.Value;
	}

	public Task ApplyConfigAsync(CancellationToken cancellationToken)
	{
		// По SSH у тебя строго "как руками": sudo wg-quick down/up.
		// Поэтому не завязываемся на ApplyWithSyncConf вообще.
		return ApplyConfigWithDownUpAsync(cancellationToken);
	}

	public Task ApplyConfigWithSyncConfAsync(CancellationToken cancellationToken)
	{
		// Чтобы "soft refresh" в телеге не ломался, по SSH делаем то же самое.
		return ApplyConfigWithDownUpAsync(cancellationToken);
	}

	public async Task ApplyConfigWithDownUpAsync(CancellationToken cancellationToken)
	{
		var interfaceName = _wg.InterfaceName;

		// Ровно как руками:
		// sudo wg-quick down wg0
		// sudo wg-quick up wg0
		var cmd =
			$"sudo wg-quick down {EscapeSh(interfaceName)} || true; " +
			$"sudo wg-quick up {EscapeSh(interfaceName)}";

		await RunSshCommandAsync(cmd, cancellationToken);
	}

	private SshClient CreateSshClient()
	{
		var methods = new List<AuthenticationMethod>();

		if (!string.IsNullOrWhiteSpace(_ssh.PrivateKeyPath))
		{
			var keyFile = new PrivateKeyFile(_ssh.PrivateKeyPath);
			methods.Add(new PrivateKeyAuthenticationMethod(_ssh.Username, keyFile));
		}

		if (!string.IsNullOrWhiteSpace(_ssh.Password))
		{
			methods.Add(new PasswordAuthenticationMethod(_ssh.Username, _ssh.Password));
		}

		if (methods.Count == 0)
		{
			throw new InvalidOperationException("WireGuardSsh: задайте Password или PrivateKeyPath.");
		}

		var connectionInfo = new ConnectionInfo(_ssh.Host, _ssh.Port, _ssh.Username, methods.ToArray());
		return new SshClient(connectionInfo);
	}

	private async Task RunSshCommandAsync(string command, CancellationToken cancellationToken)
	{
		// SSH.NET не поддерживает CancellationToken “нативно”, делаем best-effort.
		await using (cancellationToken.Register(() =>
		             {
			             // best-effort: в реальности команду на удалённой стороне это может не остановить
		             }))
		{
			using var ssh = CreateSshClient();
			await ssh.ConnectAsync(cancellationToken);

			var cmd = ssh.CreateCommand(command);
			cmd.Execute();
			var exit = cmd.ExitStatus;
			var err = cmd.Error;

			ssh.Disconnect();

			if (exit != 0)
			{
				throw new InvalidOperationException($"SSH command failed (exit={exit}). Error: {err}");
			}

			await Task.CompletedTask;
		}
	}

	private string EscapeSh(string s)
		=> "'" + s.Replace("'", "'\"'\"'") + "'";
}