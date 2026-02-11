namespace WireGuard.Models.Options;

public sealed class DockerWireGuardRunnerOptions
{
	/// <summary>
	/// Включить выполнение команд через Docker API.
	/// </summary>
	public bool Enabled { get; init; }

	/// <summary>
	/// Путь к сокету Docker внутри контейнера приложения.
	/// </summary>
	public string DockerSocketPath { get; init; } = "/var/run/docker.sock";

	/// <summary>
	/// Имя контейнера wg-easy (откуда берём wg0.conf).
	/// </summary>
	public string TargetContainerName { get; init; } = "wg-easy";

	/// <summary>
	/// Образ контейнера, в котором есть wg/wg-quick (для выполнения команд в host netns).
	/// </summary>
	public string HelperImage { get; init; } = "ghcr.io/linuxserver/wireguard:latest";

	/// <summary>
	/// Путь к директории WireGuard на хосте.
	/// </summary>
	public string HostWireGuardDir { get; init; } = "/etc/wireguard";
}