using System;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using SpecialRoles.Core;
using SpecialRoles.Roles;
using UnityEngine;

namespace SpecialRoles.Game;

// Alle Patches fangen eigene Fehler ab, damit die Mod das Spiel nie kaputt macht.
// Parameter über die Position (__0, __1 …), damit Namensänderungen im Spiel die Patches nicht brechen.
// Nur einfache Signaturen patchen (keine Nullable-/Il2Cpp-Listen-Parameter an virtuellen Methoden – siehe CrewStats).

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
    public static void Prefix()
    {
        Safe.Run(Hud.Reset);
        Safe.Run(Jester.Reset); // Narr-Sieg der letzten Runde ist spätestens jetzt vorbei
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
internal static class HudUpdatePatch
{
    public static void Postfix() => Round.Tick(); // fängt eigene Fehler ab
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

// ---------- Kills, Tod, Wiederbeleben ----------

/// <summary>
/// Host prüft jeden normalen Kill: wachsamer Veteran → der Angreifer stirbt; Schild/Glück/Armor → Kill scheitert,
/// der Angreifer bekommt trotzdem seinen Kill-Cooldown (sonst drückt er einfach nochmal).
/// </summary>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
internal static class CheckMurderPatch
{
    public static bool Prefix(PlayerControl __instance, PlayerControl __0)
    {
        try
        {
            if (!Players.IsHost || __0 == null || !Players.IsAlive(__instance) || !Players.IsAlive(__0)) return true;
            if (!Players.IsImpostor(__instance) || Players.IsImpostor(__0)) return true; // das Spiel lehnt das selbst ab
            if (!Attack.Contact(__instance, __0)) return false;
            if (Attack.Lethal(__0)) return true;
            Attack.SendKillBlocked(__instance, __0);
            return false;
        }
        catch (Exception exc)
        {
            SpecialRolesPlugin.Logger.LogError($"Kill prüfen: {exc}");
            return true;
        }
    }
}

/// <summary>Vampir: der eigene Kill-Knopf beißt nur.</summary>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdCheckMurder))]
internal static class CmdCheckMurderPatch
{
    public static bool Prefix(PlayerControl __instance, PlayerControl __0)
    {
        try
        {
            return !Vampire.TryBite(__instance, __0);
        }
        catch (Exception exc)
        {
            SpecialRolesPlugin.Logger.LogError($"Vampir: {exc}");
            return true;
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
internal static class MurderPlayerPatch
{
    public static void Prefix(PlayerControl __instance, PlayerControl __0)
    {
        if (__0 == null) return;
        Safe.Run(() => Deaths.BeforeMurder(__instance, __0));
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
internal static class DiePatch
{
    public static void Prefix(PlayerControl __instance) => Safe.Run(() => Deaths.BeforeDie(__instance));

    public static void Postfix(PlayerControl __instance, DeathReason __0)
    {
        Safe.Run(() => Deaths.OnDied(__instance, __0));
        Safe.Run(() => Oracle.OnDeath(__instance, __0));
    }
}

/// <summary>Host: vor jedem Meeting sterben alle vom Vampir Gebissenen.</summary>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
internal static class ReportDeadBodyPatch
{
    public static void Prefix()
    {
        if (Players.IsHost) Safe.Run(Vampire.BeforeMeeting);
    }
}

// ---------- Meeting ----------

/// <summary>Host: Bürgermeister-Stimme doppelt zählen.</summary>
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CalculateVotes))]
internal static class CalculateVotesPatch
{
    public static void Postfix(MeetingHud __instance, Il2CppSystem.Collections.Generic.Dictionary<byte, int> __result)
    {
        if (__result == null) return;
        Safe.Run(() => Mayor.AddVotes(__instance, __result));
    }
}

/// <summary>Alle: zweites Stimm-Symbol für den Bürgermeister.</summary>
[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateResults))]
internal static class PopulateResultsPatch
{
    public static void Postfix(MeetingHud __instance, Il2CppStructArray<MeetingHud.VoterState> __0)
    {
        if (__0 == null) return;
        Safe.Run(() => Mayor.AddVoteIcons(__instance, __0));
    }
}

/// <summary>Erpresst: Chat-Senden im Meeting blocken (nur der eigene Client, kein Text geht raus).</summary>
[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
internal static class SendChatPatch
{
    public static bool Prefix() => ChatBlock.Allow();
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendFreeChat))]
internal static class SendFreeChatPatch
{
    public static bool Prefix() => ChatBlock.Allow();
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendQuickChat))]
internal static class SendQuickChatPatch
{
    public static bool Prefix() => ChatBlock.Allow();
}

internal static class ChatBlock
{
    public static bool Allow()
    {
        try
        {
            if (!Blackmailer.LocalSilenced()) return true;
            Blackmailer.WarnSilenced();
            return false;
        }
        catch (Exception exc)
        {
            SpecialRolesPlugin.Logger.LogError($"Chat: {exc}");
            return true;
        }
    }
}

// ---------- Narr ----------

/// <summary>Host: wird der Narr rausgewählt, gewinnt er.</summary>
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Exiled))]
internal static class ExiledPatch
{
    public static void Prefix(PlayerControl __instance) => Safe.Run(() => Jester.OnExiled(__instance));
}

/// <summary>Host: solange der Narr-Sieg läuft, kein anderes Spielende.</summary>
[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
internal static class CheckEndCriteriaPatch
{
    public static bool Prefix() => !Jester.WinPending;
}

[HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
internal static class ExileBeginPatch
{
    public static void Postfix(ExileController __instance)
    {
        Safe.Run(() =>
        {
            var text = Jester.ExileText(__instance.initData?.networkedPlayer);
            if (text != null) __instance.completeString = text;
        });
    }
}

[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
internal static class EndScreenPatch
{
    public static void Postfix(EndGameManager __instance) => Safe.Run(() => Jester.ShowEndScreen(__instance));
}

// ---------- Spielende ----------

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
internal static class GameEndPatch
{
    public static void Prefix()
    {
        Safe.Run(Jester.BeforeGameEnd);
        Round.EndGame();
    }

    public static void Postfix() => Safe.Run(Jester.AfterGameEnd);
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.ExitGame))]
internal static class ExitGamePatch
{
    public static void Prefix() => Round.EndGame();
}
