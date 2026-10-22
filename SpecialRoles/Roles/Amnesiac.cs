using AmongUs.GameOptions;
using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Vergesslicher (Neutral, im Spiel Crewmate): übernimmt an einer Leiche deren Rolle – Mod-Rolle und Spielrolle (war der
/// Tote Impostor, wird er Impostor). Der Host prüft, setzt die Spielrolle und schickt allen die neue Mod-Rolle.
/// </summary>
internal static class Amnesiac
{
    public static readonly RememberButton Button = new();

    internal sealed class RememberButton : RoleButton
    {
        protected override string Label => "Erinnern";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => 0f;
        protected override Sprite? Icon(HudManager hud) => hud.ReportButton != null ? hud.ReportButton.graphic.sprite : null;
        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => Bodies.Closest(local.GetTruePosition()) != null;

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            var body = Bodies.Closest(local.GetTruePosition());
            if (body == null) return;
            var id = body.ParentId;
            if (Players.IsHost) HostRemember(local, id);
            else Net.Send(RpcIds.Remember, writer => writer.Write(id));
        }
    }

    /// <summary>Host: Vergesslicher übernimmt die Rolle des Toten bodyId.</summary>
    public static void HostRemember(PlayerControl amnesiac, byte bodyId)
    {
        if (RoleState.Get(amnesiac.PlayerId) != RoleId.Amnesiac || !Players.IsAlive(amnesiac) || Players.InMeeting) return;
        var dead = Players.ById(bodyId);
        var body = Bodies.ById(bodyId);
        if (dead == null || dead.Data == null || !dead.Data.IsDead || body == null) return;
        if (Vector2.Distance(amnesiac.GetTruePosition(), body.TruePosition) > Bodies.Range + 1f) return;

        var newRole = RoleState.Get(bodyId);
        if (newRole == RoleId.Amnesiac) newRole = RoleId.None;
        var gameRole = Deaths.RoleBeforeDeath(bodyId) ?? RoleTypes.Crewmate;
        if (gameRole != RoleTypes.Crewmate) amnesiac.RpcSetRole(gameRole, true);

        var id = amnesiac.PlayerId;
        Net.Send(RpcIds.RoleChange, writer =>
        {
            writer.Write(id);
            writer.Write((byte)newRole);
            writer.Write(bodyId);
        });
        OnRoleChange(id, newRole, bodyId);
    }

    /// <summary>Alle Clients (vom Host).</summary>
    public static void OnRoleChange(byte playerId, RoleId role, byte bodyId)
    {
        RoleState.SetRole(playerId, role);
        Bodies.Remove(bodyId);
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.PlayerId != playerId) return;
        var info = RoleCatalog.Get(role);
        var text = info != null ? $"Du bist jetzt {Colors.Paint(role, info.Name)}!" : "Du erinnerst dich: du hast jetzt die Spielrolle des Toten.";
        Notify.Show(text, 8f);
    }
}
