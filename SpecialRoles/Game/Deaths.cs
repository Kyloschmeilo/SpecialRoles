using System.Collections.Generic;
using AmongUs.GameOptions;
using SpecialRoles.Core;
using UnityEngine;

namespace SpecialRoles.Game;

/// <summary>
/// Wer wann wo gestorben ist (alle Clients, aus MurderPlayer + Die). Für das Medium auch der Weg des Mörders kurz davor.
/// Bei Kills ohne Teleport (Biss, Bombe …) „tötet sich“ das Opfer im Spiel selbst – dann zählt, wer gebissen bzw.
/// die Bombe angeklebt hat (die RPCs gehen an alle, wie ein normaler Kill im Spiel auch).
/// </summary>
internal static class Deaths
{
    /// <summary>So lange vor dem Tod wird der Weg des Mörders aufgehoben.</summary>
    public const float PathSeconds = 12f;

    public sealed record Death(byte Victim, byte? Killer, float Time, Vector2 Position, List<Vector2> Path);

    private static readonly Dictionary<byte, Death> ByVictim = new();
    private static readonly Dictionary<byte, byte> Blame = new();
    private static readonly Dictionary<byte, byte> PendingKiller = new();
    private static readonly Dictionary<byte, RoleTypes> RoleBefore = new();

    public static Death? Of(byte victim) => ByVictim.TryGetValue(victim, out var death) ? death : null;

    /// <summary>Spielrolle vor dem Tod (für Wiederbeleben und Vergesslichen).</summary>
    public static RoleTypes? RoleBeforeDeath(byte playerId) => RoleBefore.TryGetValue(playerId, out var role) ? role : (RoleTypes?)null;

    /// <summary>victim stirbt gleich ohne Teleport, Täter ist killer (Biss, Bombe).</summary>
    public static void BlameOn(byte victim, byte killer) => Blame[victim] = killer;

    /// <summary>Die-Prefix: Spielrolle merken, bevor sie zur Geister-Rolle wird.</summary>
    public static void BeforeDie(PlayerControl player)
    {
        if (player.Data != null && player.Data.RoleType is not (RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost or RoleTypes.GuardianAngel))
            RoleBefore[player.PlayerId] = player.Data.RoleType;
    }

    /// <summary>MurderPlayer-Prefix (alle Clients): Täter merken, gestorben wird in Die (je nach Spielversion verzögert).</summary>
    public static void BeforeMurder(PlayerControl killer, PlayerControl victim) => PendingKiller[victim.PlayerId] = killer.PlayerId;

    /// <summary>Die-Postfix (alle Clients).</summary>
    public static void OnDied(PlayerControl victim, DeathReason reason)
    {
        var hadKiller = PendingKiller.Remove(victim.PlayerId, out var pending);
        if (reason != DeathReason.Kill) return;
        byte? killerId = null;
        if (hadKiller && pending != victim.PlayerId) killerId = pending;
        else if (Blame.TryGetValue(victim.PlayerId, out var blamed)) killerId = blamed;
        Blame.Remove(victim.PlayerId);
        var now = Time.time;
        var path = killerId != null ? Footprints.Snapshot(killerId.Value, now - PathSeconds, now) : new List<Vector2>();
        ByVictim[victim.PlayerId] = new Death(victim.PlayerId, killerId, now, victim.GetTruePosition(), path);
    }

    /// <summary>Wiederbelebt: alter Tod zählt nicht mehr.</summary>
    public static void Forget(byte playerId)
    {
        ByVictim.Remove(playerId);
        Blame.Remove(playerId);
    }

    public static void Reset()
    {
        ByVictim.Clear();
        Blame.Clear();
        PendingKiller.Clear();
        RoleBefore.Clear();
    }
}
