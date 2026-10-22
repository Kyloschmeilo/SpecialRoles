using System.Collections.Generic;
using AmongUs.GameOptions;
using SpecialRoles.Core;
using UnityEngine;

namespace SpecialRoles.Game;

internal static class Players
{
    // Kill-Reichweite des Spiels (Einstellung „Kill Distance“: kurz/mittel/lang)
    private static readonly float[] KillDistances = { 1f, 1.8f, 2.5f };

    public static PlayerControl? ById(byte playerId)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
            if (player != null && player.PlayerId == playerId)
                return player;
        return null;
    }

    public static IEnumerable<PlayerControl> All()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
            if (player != null)
                yield return player;
    }

    public static IEnumerable<PlayerControl> Alive()
    {
        foreach (var player in All())
            if (IsAlive(player))
                yield return player;
    }

    public static bool IsAlive(PlayerControl? player) =>
        player != null && player.Data != null && !player.Data.IsDead && !player.Data.Disconnected;

    public static bool IsImpostor(PlayerControl player) =>
        player.Data != null && RoleManager.IsImpostorRole(player.Data.RoleType);

    public static bool IsHost => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;

    public static bool InMeeting => MeetingHud.Instance != null || ExileController.Instance != null;

    /// <summary>Team für Anzeigen: Impostor (Spiel), sonst Neutral (Mod-Rolle) oder Crew.</summary>
    public static RoleTeam TeamOf(PlayerControl player)
    {
        if (IsImpostor(player)) return RoleTeam.Impostor;
        return RoleCatalog.Get(RoleState.Get(player.PlayerId))?.Team == RoleTeam.Neutral ? RoleTeam.Neutral : RoleTeam.Crew;
    }

    public static float KillDistance()
    {
        var options = GameOptionsManager.Instance?.CurrentGameOptions;
        var index = options?.GetInt(Int32OptionNames.KillDistance) ?? 1;
        return KillDistances[System.Math.Clamp(index, 0, KillDistances.Length - 1)];
    }

    public static float KillCooldown()
    {
        var options = GameOptionsManager.Instance?.CurrentGameOptions;
        return options?.GetFloat(FloatOptionNames.KillCooldown) ?? 25f;
    }

    /// <summary>Nächster lebender Mitspieler in Reichweite (nicht im Vent).</summary>
    public static PlayerControl? ClosestTarget(PlayerControl local, float range, System.Func<PlayerControl, bool>? filter = null) =>
        ClosestTo(local.GetTruePosition(), range, p => p.PlayerId != local.PlayerId && (filter == null || filter(p)));

    public static PlayerControl? ClosestTo(Vector2 position, float range, System.Func<PlayerControl, bool>? filter = null)
    {
        PlayerControl? best = null;
        var bestDistance = range;
        foreach (var player in Alive())
        {
            if (player.inVent || (filter != null && !filter(player))) continue;
            var distance = Vector2.Distance(position, player.GetTruePosition());
            if (distance <= bestDistance)
            {
                best = player;
                bestDistance = distance;
            }
        }
        return best;
    }

    /// <summary>Spielerfarbe (echte Farbe, nicht die einer Tarnung).</summary>
    public static Color ColorOf(byte playerId)
    {
        var info = GameData.Instance?.GetPlayerById(playerId);
        var colors = Palette.PlayerColors;
        if (info == null || info.DefaultOutfit == null || colors == null) return Color.white;
        var id = info.DefaultOutfit.ColorId;
        return id >= 0 && id < colors.Length ? colors[id] : Color.white;
    }

    /// <summary>Name in Spielerfarbe für Hinweise.</summary>
    public static string ColoredName(byte playerId)
    {
        var info = GameData.Instance?.GetPlayerById(playerId);
        var name = info?.DefaultOutfit?.PlayerName ?? info?.PlayerName ?? "?";
        return $"<color=#{Hex(ColorOf(playerId))}>{name}</color>";
    }

    public static string Hex(Color color)
    {
        Color32 c = color;
        return $"{c.r:X2}{c.g:X2}{c.b:X2}";
    }
}
