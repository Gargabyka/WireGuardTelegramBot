using Application.Repository;
using Application.Services.Interfaces;
using Domain.Interfaces.Repository;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DiContainerExtensions
{
	public static void AddApplicationConfiguration(this IServiceCollection services)
	{
		services.AddScoped<ITelegramUserRepository, TelegramUserRepository>();
		services.AddScoped<IWireGuardPeerRepository, WireGuardPeerRepository>();
		services.AddScoped<IWireGuardUserRepository, WireGuardUserRepository>();
		services.AddScoped<TelegramUserService>();
	}
}