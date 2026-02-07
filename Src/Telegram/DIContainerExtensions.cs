using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Models;
using Telegram.Service;
using Telegram.Service.Callback;
using Telegram.Service.Factory;
using Telegram.Service.Interface;
using Telegram.Service.TextFlows;
using Telegram.Service.Updates;
using Telegram.Store;
using Telegram.Store.Interfaces;

namespace Telegram;

public static class DiContainerExtensions
{
    public static void AddTelegramConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<TelegramOptions>()
            .Bind(configuration.GetSection("Telegram"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Token), "Токен бота обязателен для заполнения")
            .Validate(o => o.AdminIds.Length > 0, "Список админов обязателен для заполнения")
            .ValidateOnStart();

        services.AddSingleton<ITelegramBotClient>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TelegramOptions>>().Value;
            return new TelegramBotClient(opts.Token);
        });

        services.AddSingleton<IAddClientStateStore, InMemoryAddClientStateStore>();
        services.AddSingleton<ITelegramHandler, TelegramHandler>();
        services.AddHostedService<TelegramLongPollingService>();

        AddTelegramUpdate(services);
        AddTelegramCallback(services);
        AddTelegramTextFlows(services);
        AddTelegramStores(services);
    }

    private static void AddTelegramUpdate(IServiceCollection services)
    {
        services.AddSingleton<TelegramUpdateFactory>();
        services.AddSingleton<StartTelegramUpdateHandler>();
    }

    private static void AddTelegramCallback(IServiceCollection services)
    {
        services.AddSingleton<TelegramCallbackFactory>();
        services.AddSingleton<ITelegramCallbackHandler, GetInfoTelegramCallbackHandler>();
        services.AddSingleton<ITelegramCallbackHandler, DownloadConfigTelegramCallbackHandler>();
        services.AddSingleton<ITelegramCallbackHandler, ListTelegramCallbackHandler>();
        services.AddSingleton<ITelegramCallbackHandler, DeleteClientAskConfirmCallbackHandler>();
        services.AddSingleton<ITelegramCallbackHandler, DeleteClientConfirmCallbackHandler>();
        services.AddSingleton<ITelegramCallbackHandler, MainMenuTelegramCallbackHandler>();
        services.AddSingleton<ITelegramCallbackHandler, AddClientStartTelegramCallbackHandler>();
        services.AddSingleton<ITelegramCallbackHandler, AddClientCancelTelegramCallbackHandler>();
    }

    private static void AddTelegramTextFlows(IServiceCollection services)
    {
        services.AddSingleton<ITelegramTextFlowHandler, AddClientNameTextFlowHandler>();
    }

    private static void AddTelegramStores(IServiceCollection services)
    {
        services.AddSingleton<IAddClientStateStore, InMemoryAddClientStateStore>();
    }
}