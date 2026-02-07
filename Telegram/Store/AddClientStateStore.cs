using System.Collections.Concurrent;
using WireGuardTelegramBot.Telegram.Store.Interfaces;

namespace WireGuardTelegramBot.Telegram.Store;

public sealed class InMemoryAddClientStateStore : IAddClientStateStore
{
    private readonly ConcurrentDictionary<long, byte> _waiting = new();

    public void Begin(long chatId) => _waiting[chatId] = 1;

    public bool IsWaitingName(long chatId) => _waiting.ContainsKey(chatId);

    public void Clear(long chatId) => _waiting.TryRemove(chatId, out _);
}