using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WireGuard.Models;
using WireGuard.Services;
using WireGuard.Services.Interface;

namespace WireGuard;

public static class DiContainerExtensions
{
    public static void AddWireguardConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<WireguardApiOptions>()
            .Bind(configuration.GetSection("WireGuardApi"))
            .ValidateOnStart();

        services.AddOptions<WireGuardOptions>()
            .Bind(configuration.GetSection("WireGuard"))
            .ValidateOnStart();

        services.AddSingleton<IWireguardService, WireGuardService>();
        services.AddSingleton<IWireGuardApiService, WireGuardApiService>();
        services.AddSingleton<HttpClient>();
    }
}