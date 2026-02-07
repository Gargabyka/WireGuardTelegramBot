using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using WireGuard.Services.Interface;

namespace WireGuard.Services;

public class WireGuardService : IWireguardService
{
    private readonly WireGuardOptions _wireGuardOptions;

    public WireGuardService(IOptions<WireGuardOptions> wireGuardOptions)
    {
        _wireGuardOptions = wireGuardOptions.Value;
    }

    public async Task ApplyConfigAsync(CancellationToken cancellationToken)
    {
        if (_wireGuardOptions.ApplyWithSyncConf)
        {
            var stripped = await RunAsync(
                fileName: "wg-quick",
                arguments: $"strip {_wireGuardOptions.InterfaceName}",
                cancellationToken: cancellationToken);

            var tempFile = Path.Combine(
                Path.GetTempPath(),
                $"{_wireGuardOptions.InterfaceName}_{Guid.NewGuid():N}.conf");

            try
            {
                await File.WriteAllTextAsync(tempFile, stripped, Encoding.UTF8, cancellationToken);

                await RunAsync(
                    fileName: "wg",
                    arguments: $"syncconf {_wireGuardOptions.InterfaceName} {tempFile}",
                    cancellationToken: cancellationToken);

                return;
            }
            finally
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                { /* ignore */
                }
            }
        }

        await RunAsync("wg-quick", $"down {_wireGuardOptions.InterfaceName}", cancellationToken, allowFailure: true);
        await RunAsync("wg-quick", $"up {_wireGuardOptions.InterfaceName}", cancellationToken);
    }

    private static async Task<string> RunAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken,
        bool allowFailure = false)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start process: {fileName}");
        var stdout = await p.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = await p.StandardError.ReadToEndAsync(cancellationToken);
        await p.WaitForExitAsync(cancellationToken);

        if (p.ExitCode != 0 && !allowFailure)
        {
            throw new InvalidOperationException($"Command failed: {fileName} {arguments}. {stderr}");
        }

        return string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
    }
}