using MegaCrit.Sts2.Core.Multiplayer.Game;
using RefinedGem.Multiplayer;

namespace RefinedGem.Services;

internal static class RefinedPoolSyncService
{
    private static readonly object Lock = new();
    private static INetGameService? _registeredNet;
    private static MessageHandlerDelegate<RefinedPoolSyncMessage>? _handler;

    public static void EnsureHandlerRegistered(INetGameService net)
    {
        lock (Lock)
        {
            if (ReferenceEquals(_registeredNet, net))
                return;

            if (_registeredNet is not null && _handler is not null)
            {
                try
                {
                    _registeredNet.UnregisterMessageHandler(_handler);
                }
                catch
                {
                    // Net service may already be torn down.
                }
            }

            _handler = OnPoolSyncMessage;
            net.RegisterMessageHandler(_handler);
            _registeredNet = net;
        }
    }

    public static void UnregisterHandler()
    {
        lock (Lock)
        {
            if (_registeredNet is not null && _handler is not null)
            {
                try
                {
                    _registeredNet.UnregisterMessageHandler(_handler);
                }
                catch
                {
                    // Net service may already be torn down.
                }
            }

            _registeredNet = null;
            _handler = null;
        }
    }

    /// <summary>
    /// Publishes this peer's pool snapshot. After the run is frozen, resends the session snapshot
    /// instead of re-reading refined_pool.json (so mid-run file edits cannot diverge peers).
    /// </summary>
    public static void BroadcastLocalPool(INetGameService net)
    {
        EnsureHandlerRegistered(net);

        IReadOnlyList<string> cardIds;
        if (RefinedPoolSessionStore.IsFrozen
            && RefinedPoolSessionStore.TryGet(net.NetId, out var frozenLocal))
        {
            cardIds = frozenLocal;
        }
        else
        {
            cardIds = RefinedPoolFileStore.GetCardIds();
            RefinedPoolSessionStore.Set(net.NetId, cardIds);
            RefinedPoolService.InvalidatePoolCache();
        }

        var message = new RefinedPoolSyncMessage
        {
            cardIds = cardIds.ToList(),
        };
        net.SendMessage(message);
    }

    private static void OnPoolSyncMessage(RefinedPoolSyncMessage message, ulong senderId)
    {
        RefinedPoolSessionStore.Set(senderId, message.cardIds ?? []);
        RefinedPoolService.InvalidatePoolCache();
        RefinedGemEntry.Logger.Info(
            $"Received refined pool snapshot from {senderId} ({message.cardIds?.Count ?? 0} cards).");
    }
}
