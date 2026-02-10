namespace WireGuard.Models;

public sealed class DockerWireGuardRunnerOptions
{
	/// <summary>
	/// Включить выполнение команд через helper-контейнер (Docker API).
	/// </summary>
	public bool Enabled { get; init; }

	/// <summary>
	/// Образ, в котором есть wg и wg-quick.
	/// </summary>
	public string Image { get; init; } = "ghcr.io/linuxserver/wireguard:latest";

	/// <summary>
	/// Путь к сокету Docker внутри контейнера приложения.
	/// </summary>
	public string DockerSocketPath { get; init; } = "/var/run/docker.sock";

	/// <summary>
	/// Путь к wireguard конфигам на хосте, который будет примонтирован в helper.
	/// </summary>
	public string HostWireGuardDir { get; init; } = "/etc/wireguard";
}