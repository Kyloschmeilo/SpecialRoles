using System.Collections.Generic;
using AmongUs.GameOptions;

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

    public static bool IsAlive(PlayerControl? player) =>
        player != null && player.Data != null && !player.Data.IsDead && !player.Data.Disconnected;

    public static bool IsImpostor(PlayerControl player) =>
        player.Data != null && RoleManager.IsImpostorRole(player.Data.RoleType);

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
    public static PlayerControl? ClosestTarget(PlayerControl local, float range)
    {
        PlayerControl? best = null;
        var bestDistance = range;
        var position = local.GetTruePosition();
        foreach (var player in All())
        {
            if (player.PlayerId == local.PlayerId || !IsAlive(player) || player.inVent) continue;
            var distance = UnityEngine.Vector2.Distance(position, player.GetTruePosition());
            if (distance <= bestDistance)
            {
                best = player;
                bestDistance = distance;
            }
        }
        return best;
    }
}
