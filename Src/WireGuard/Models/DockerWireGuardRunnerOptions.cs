namespace WireGuard.Models;

public sealed class DockerWireGuardRunnerOptions
{
	/// <summary>
	/// Включить выполнение команд через helper-контейнер (Docker API).
	/// </summary>
	public bool Enabled { get; init; }

	/// <summary>
	/// Путь к сокету Docker внутри контейнера приложения.
	/// </summary>
	public string DockerSocketPath { get; init; } = "/var/run/docker.sock";

	/// <summary>
	/// Имя контейнера, внутри которого нужно выполнять wg/wg-quick (например, wg-easy).
	/// </summary>
	public string TargetContainerName { get; init; } = "wg-easy";
}