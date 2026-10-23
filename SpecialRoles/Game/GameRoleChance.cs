using System;
using AmongUs.GameOptions;
using HarmonyLib;
using SpecialRoles.Core;
using UnityEngine;

namespace SpecialRoles.Game;

/// <summary>
/// Spiel-Rollen seltener (Kilian, 23.10.2026: „Custom Rollen etwas wahrscheinlicher als die normalen“). Nur als Host und
/// nur während RoleManager.SelectRoles: fragt das Spiel die Chance einer Spiel-Rolle ab (Engineer, Shapeshifter …),
/// bekommt es nur Settings.GameRoleShare % davon. Wer dadurch keine Spiel-Rolle bekommt, ist normaler Crewmate/Impostor
/// und bekommt danach eine Mod-Rolle. Die Lobby-Einstellungen selbst ändern sich nicht; für alle Spieler gleich.
/// Gepatcht wird nur GetChancePerGame(RoleTypes) – einfache Signatur, Muster wie CrewStats' RoleShuffle.
/// </summary>
internal static class GameRoleChance
{
    private static bool _active;
    private static int _frame;
    private static int _thread; // nur der Spiel-Thread (Time.frameCount geht nur dort)
    private static int _scaled;

    /// <summary>Nach PatchAll: einzeln patchen, damit eine fehlende Methode nicht die ganze Mod stoppt.</summary>
    public static void Install(Harmony harmony)
    {
        var postfix = new HarmonyMethod(AccessTools.Method(typeof(GameRoleChance), nameof(ChancePostfix)));
        var patched = 0;
        foreach (var type in new[] { typeof(RoleOptionsCollectionV10), typeof(RoleOptionsCollectionV11), typeof(RoleOptionsCollectionV12) })
        {
            try
            {
                var method = AccessTools.Method(type, nameof(IRoleOptionsCollection.GetChancePerGame));
                if (method == null) continue;
                harmony.Patch(method, postfix: postfix);
                patched++;
            }
            catch (Exception exc)
            {
                SpecialRolesPlugin.Logger.LogWarning($"Spiel-Rollen-Chance: {type.Name} nicht gepatcht ({exc.Message}).");
            }
        }
        SpecialRolesPlugin.Logger.LogInfo($"Spiel-Rollen-Chance: {patched} Rollen-Optionen gepatcht.");
    }

    public static void BeforeSelectRoles()
    {
        var client = AmongUsClient.Instance;
        _active = client != null && client.AmHost && Settings.GameRoleShare < 100;
        _frame = Time.frameCount;
        _thread = Environment.CurrentManagedThreadId;
        _scaled = 0;
    }

    public static void AfterSelectRoles()
    {
        if (!_active) return;
        _active = false;
        SpecialRolesPlugin.Logger.LogInfo(_scaled > 0
            ? $"Spiel-Rollen mit {Settings.GameRoleShare} % ihrer Chance gewürfelt ({_scaled} Abfragen)."
            : "Spiel-Rollen-Chance: das Spiel hat keine Chance abgefragt – Spiel-Rollen wie in der Lobby.");
    }

    private static void ChancePostfix(RoleTypes __0, ref int __result)
    {
        try
        {
            if (!_active || Environment.CurrentManagedThreadId != _thread) return;
            if (Time.frameCount != _frame)
            {
                _active = false; // SelectRoles ist ohne Postfix zu Ende gegangen
                return;
            }
            if (__0 is RoleTypes.Crewmate or RoleTypes.Impostor || __result <= 0) return;
            __result = RoleAssigner.ScaleGameRoleChance(__result, Settings.GameRoleShare);
            _scaled++;
        }
        catch (Exception exc)
        {
            SpecialRolesPlugin.Logger.LogError($"Spiel-Rollen-Chance: {exc}");
        }
    }
}
