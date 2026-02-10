using System.Net;
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

		services.AddOptions<DockerWireGuardRunnerOptions>()
			.Bind(configuration.GetSection("WireGuardDocker"))
			.ValidateOnStart();

		services.AddScoped<IWireguardService>(sp =>
		{
			var docker = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<DockerWireGuardRunnerOptions>>().Value;
			if (docker.Enabled)
			{
				return ActivatorUtilities.CreateInstance<DockerWireguardService>(sp);
			}

			return ActivatorUtilities.CreateInstance<WireGuardService>(sp);
		});

		services.AddHttpClient<IWireGuardApiService, WireGuardApiService>((sp, client) =>
			{
				var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<WireguardApiOptions>>().Value;
				client.BaseAddress = new Uri(options.ApiUrl);
			})
			.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
			{
				CookieContainer = new CookieContainer(),
				AutomaticDecompression = DecompressionMethods.All,
			});
	}
}