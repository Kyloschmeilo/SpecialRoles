using System.Collections.Generic;
using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Orakel (Crew): wählt einen Spieler. Stirbt das Orakel (Kill oder Rauswählen), erfahren alle das Team des
/// zuletzt gewählten Spielers – als Hinweis und in den folgenden Meetings hinter seinem Namen.
/// </summary>
internal static class Oracle
{
    // Alle Clients: Orakel → gewählter Spieler; verratene Spieler → Team
    private static readonly Dictionary<byte, byte> Chosen = new();
    private static readonly Dictionary<byte, RoleTeam> Revealed = new();

    public static readonly ChooseButton Button = new();

    internal sealed class ChooseButton : RoleButton
    {
        protected override string Label => "Wählen";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => RoleState.Settings.Get(SettingId.OracleCooldown);

        protected override PlayerControl? FindTarget(PlayerControl local) =>
            Players.ClosestTarget(local, Players.KillDistance(),
                p => !Chosen.TryGetValue(local.PlayerId, out var chosen) || chosen != p.PlayerId);

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            if (target == null) return;
            var id = target.PlayerId;
            Net.Send(RpcIds.OracleChoose, writer => writer.Write(id));
            OnChoose(local, id);
            Notify.Show(Players.ColoredName(id) + Colors.Paint(RoleId.Oracle, " gewählt – stirbst du, erfahren alle sein Team."));
        }
    }

    public static void OnChoose(PlayerControl oracle, byte targetId)
    {
        if (RoleState.Get(oracle.PlayerId) != RoleId.Oracle || !Players.IsAlive(oracle)) return;
        Chosen[oracle.PlayerId] = targetId;
    }

    /// <summary>Die-Postfix (alle Clients).</summary>
    public static void OnDeath(PlayerControl player, DeathReason reason)
    {
        if (reason == DeathReason.Disconnect || RoleState.Get(player.PlayerId) != RoleId.Oracle) return;
        if (!Chosen.Remove(player.PlayerId, out var targetId)) return;
        var target = Players.ById(targetId);
        if (target == null) return;
        var team = Players.TeamOf(target);
        Revealed[targetId] = team;
        Notify.Show(Colors.Paint(RoleId.Oracle, "Das Orakel ist tot: ") + Players.ColoredName(targetId) + $" ist {TeamText(team)}.", 10f);
    }

    /// <summary>Text hinter dem Namen im Meeting (leer = nichts verraten).</summary>
    public static string MeetingTag(byte playerId) =>
        Revealed.TryGetValue(playerId, out var team) ? TeamText(team) : "";

    private static string TeamText(RoleTeam team) => team switch
    {
        RoleTeam.Impostor => "<color=#FF1919>Impostor</color>",
        RoleTeam.Neutral => "<color=#AAAAAA>neutral</color>",
        _ => "<color=#8CFFFF>Crewmate</color>",
    };

    public static void Reset()
    {
        Chosen.Clear();
        Revealed.Clear();
    }
}
