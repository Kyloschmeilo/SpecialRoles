using System;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using SpecialRoles.Core;
using SpecialRoles.Roles;
using UnityEngine;

namespace SpecialRoles.Game;

// Alle Patches fangen eigene Fehler ab, damit die Mod das Spiel nie kaputt macht.
// Parameter über die Position (__0, __1 …), damit Namensänderungen im Spiel die Patches nicht brechen.

/// <summary>Host: nach der Rollenvergabe des Spiels die Mod-Rollen verteilen. Läuft als Letztes (nach CrewStats).</summary>
[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
internal static class SelectRolesPatch
{
    [HarmonyPriority(Priority.Last)]
    public static void Postfix() => Safe.Run(Distribution.HostAssign);
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
internal static class HandleRpcPatch
{
    // false = Original überspringen (eigene RPC-IDs kennt das Spiel nicht)
    public static bool Prefix(PlayerControl __instance, byte __0, MessageReader __1)
    {
        if (!Net.IsOurs(__0)) return true;
        Safe.Run(() => Net.Handle(__instance, __0, __1));
        return false;
    }
}

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
internal static class IntroPatch
{
    public static void Prefix() => Safe.Run(Hud.Reset);
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
internal static class HudUpdatePatch
{
    public static void Postfix() => Hud.Tick(); // fängt eigene Fehler ab
}

[HarmonyPatch(typeof(TaskPanelBehaviour), nameof(TaskPanelBehaviour.SetTaskText))]
internal static class TaskTextPatch
{
    public static void Prefix(ref string __0)
    {
        try
        {
            __0 = NameTag.TaskHeader() + __0;
        }
        catch (Exception exc)
        {
            SpecialRolesPlugin.Logger.LogError($"Aufgabenliste: {exc}");
        }
    }
}

/// <summary>Nachtsicht: bei Licht-Sabotage volle Sicht wie ohne Sabotage.</summary>
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CalculateLightRadius))]
internal static class LightRadiusPatch
{
    public static void Postfix(ShipStatus __instance, NetworkedPlayerInfo __0, ref float __result)
    {
        try
        {
            if (__0 == null || __0.IsDead || RoleState.Get(__0.PlayerId) != RoleId.NightVision) return;
            var options = GameOptionsManager.Instance?.CurrentGameOptions;
            if (options == null) return;
            __result = Mathf.Max(__result, __instance.MaxLightRadius * options.GetFloat(FloatOptionNames.CrewLightMod));
        }
        catch (Exception exc)
        {
            SpecialRolesPlugin.Logger.LogError($"Nachtsicht: {exc}");
        }
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
internal static class GameEndPatch
{
    public static void Prefix() => EndRound();

    internal static void EndRound()
    {
        Safe.Run(Hud.Reset);
        Safe.Run(Sheriff.Reset);
        Safe.Run(RoleState.Reset);
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
internal static class ExitGamePatch
{
    public static void Prefix() => GameEndPatch.EndRound();
}
