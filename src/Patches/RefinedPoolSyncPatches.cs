using HarmonyLib;
using MegaCrit.Sts2.Core.Daily;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Runs;
using RefinedGem.Services;

namespace RefinedGem.Patches;

[HarmonyPatch(typeof(StartRunLobby), MethodType.Constructor,
    [typeof(GameMode), typeof(INetGameService), typeof(IStartRunLobbyListener), typeof(int)])]
internal static class StartRunLobbyCtorPatch
{
    [HarmonyPostfix]
    private static void Postfix(StartRunLobby __instance) =>
        RefinedPoolSyncHooks.OnLobbyConstructed(__instance.NetService, __instance);
}

[HarmonyPatch(typeof(StartRunLobby), MethodType.Constructor,
    [typeof(GameMode), typeof(INetGameService), typeof(IStartRunLobbyListener), typeof(TimeServerResult), typeof(int)])]
internal static class StartRunLobbyCtorDailyPatch
{
    [HarmonyPostfix]
    private static void Postfix(StartRunLobby __instance) =>
        RefinedPoolSyncHooks.OnLobbyConstructed(__instance.NetService, __instance);
}

[HarmonyPatch(typeof(StartRunLobby), nameof(StartRunLobby.SetReady))]
internal static class StartRunLobbySetReadyPatch
{
    [HarmonyPrefix]
    private static void Prefix(StartRunLobby __instance, bool ready)
    {
        if (!ready)
            return;

        RefinedPoolSyncService.BroadcastLocalPool(__instance.NetService);
    }
}

[HarmonyPatch(typeof(StartRunLobby), "HandlePlayerJoinedMessage")]
internal static class StartRunLobbyPlayerJoinedPatch
{
    [HarmonyPostfix]
    private static void Postfix(StartRunLobby __instance) =>
        RefinedPoolSyncService.BroadcastLocalPool(__instance.NetService);
}

[HarmonyPatch(typeof(StartRunLobby), "BeginRunLocally")]
internal static class StartRunLobbyBeginRunLocallyPatch
{
    [HarmonyPrefix]
    private static void Prefix() => RefinedPoolSessionStore.Freeze();
}

[HarmonyPatch(typeof(StartRunLobby), nameof(StartRunLobby.CleanUp))]
internal static class StartRunLobbyCleanUpPatch
{
    [HarmonyPostfix]
    private static void Postfix(bool disconnectSession)
    {
        if (disconnectSession)
            RefinedPoolSyncHooks.OnLobbyDisconnected();
    }
}

[HarmonyPatch(typeof(LoadRunLobby))]
internal static class LoadRunLobbyCtorPatch
{
    private static IEnumerable<System.Reflection.MethodBase> TargetMethods() =>
        typeof(LoadRunLobby).GetConstructors();

    [HarmonyPostfix]
    private static void Postfix(LoadRunLobby __instance) =>
        RefinedPoolSyncHooks.OnLobbyConstructed(__instance.NetService);
}

[HarmonyPatch(typeof(LoadRunLobby), nameof(LoadRunLobby.SetReady))]
internal static class LoadRunLobbySetReadyPatch
{
    [HarmonyPrefix]
    private static void Prefix(LoadRunLobby __instance, bool ready)
    {
        if (!ready)
            return;

        RefinedPoolSyncService.BroadcastLocalPool(__instance.NetService);
    }
}

[HarmonyPatch(typeof(LoadRunLobby), "HandlePlayerReconnectedMessage")]
internal static class LoadRunLobbyPlayerReconnectedPatch
{
    [HarmonyPostfix]
    private static void Postfix(LoadRunLobby __instance) =>
        RefinedPoolSyncService.BroadcastLocalPool(__instance.NetService);
}

[HarmonyPatch(typeof(LoadRunLobby), "BeginRunLocally")]
internal static class LoadRunLobbyBeginRunLocallyPatch
{
    [HarmonyPrefix]
    private static void Prefix() => RefinedPoolSessionStore.Freeze();
}

[HarmonyPatch(typeof(LoadRunLobby), nameof(LoadRunLobby.CleanUp))]
internal static class LoadRunLobbyCleanUpPatch
{
    [HarmonyPostfix]
    private static void Postfix(bool disconnectSession)
    {
        if (disconnectSession)
            RefinedPoolSyncHooks.OnLobbyDisconnected();
    }
}

[HarmonyPatch(typeof(RunLobby), MethodType.Constructor,
    [typeof(GameMode), typeof(INetGameService), typeof(IRunLobbyListener), typeof(IPlayerCollection), typeof(IEnumerable<RunLobbyPlayer>)])]
internal static class RunLobbyCtorPatch
{
    [HarmonyPostfix]
    private static void Postfix(RunLobby __instance, INetGameService netService)
    {
        RefinedPoolSyncService.EnsureHandlerRegistered(netService);
        RefinedPoolSyncHooks.AttachRunLobby(__instance, netService);
    }
}

internal static class RefinedPoolSyncHooks
{
    private static readonly HashSet<StartRunLobby> AttachedStartLobbies = [];
    private static readonly HashSet<RunLobby> AttachedRunLobbies = [];

    public static void OnLobbyConstructed(INetGameService net, StartRunLobby? startLobby = null)
    {
        RefinedPoolSessionStore.Clear();
        RefinedPoolSyncService.EnsureHandlerRegistered(net);
        RefinedPoolService.InvalidatePoolCache();

        if (startLobby is null || !AttachedStartLobbies.Add(startLobby))
            return;

        startLobby.PlayerConnected += _ =>
        {
            try
            {
                RefinedPoolSyncService.BroadcastLocalPool(startLobby.NetService);
            }
            catch (Exception ex)
            {
                RefinedGemEntry.Logger.Warn($"Failed to broadcast refined pool on player connected: {ex.Message}");
            }
        };
    }

    public static void OnLobbyDisconnected()
    {
        RefinedPoolSyncService.UnregisterHandler();
        RefinedPoolSessionStore.Clear();
        RefinedPoolService.InvalidatePoolCache();
        AttachedStartLobbies.Clear();
        AttachedRunLobbies.Clear();
    }

    public static void AttachRunLobby(RunLobby lobby, INetGameService netService)
    {
        if (!AttachedRunLobbies.Add(lobby))
            return;

        lobby.PlayerRejoined += _ =>
        {
            try
            {
                RefinedPoolSyncService.BroadcastLocalPool(netService);
            }
            catch (Exception ex)
            {
                RefinedGemEntry.Logger.Warn($"Failed to broadcast refined pool on rejoin: {ex.Message}");
            }
        };
    }
}
