namespace WireGuard.Models.Options;

public sealed class WireGuardSshOptions
{
	/// <summary>
	/// Включить применение WireGuard через SSH.
	/// </summary>
	public bool Enabled { get; init; }

	/// <summary>
	/// Url SSH
	/// </summary>
	public required string Host { get; init; }

	/// <summary>
	/// Порт SSH
	/// </summary>
	public int Port { get; init; } = 22;

	/// <summary>
	/// Пользователь SSH
	/// </summary>
	public required string Username { get; init; }

	/// <summary>
	/// Либо Password, либо PrivateKeyPath (хотя бы одно).
	/// </summary>
	public string? Password { get; init; }

	/// <summary>
	/// Путь к приватному ключу (если используешь ключевую аутентификацию).
	/// </summary>
	public string? PrivateKeyPath { get; init; }
}