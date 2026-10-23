using AmongUs.GameOptions;
using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Altruist (Crew): belebt eine Leiche wieder und stirbt dabei selbst. Der Altruist fragt den Host; der Host prüft,
/// tötet den Altruisten und schickt allen das Wiederbeleben (Spielrolle von vor dem Tod wird wiederhergestellt).
/// </summary>
internal static class Altruist
{
    public static readonly ReviveButton Button = new();

    internal sealed class ReviveButton : RoleButton
    {
        protected override string Label => "Beleben";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => 0f;
        protected override Sprite? Icon(HudManager hud) => hud.ReportButton != null ? hud.ReportButton.graphic.sprite : null;
        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => Bodies.Closest(local.GetTruePosition()) != null;

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            var body = Bodies.Closest(local.GetTruePosition());
            if (body == null) return;
            var id = body.ParentId;
            if (Players.IsHost) HostRevive(local, id);
            else Net.Send(RpcIds.ReviveRequest, writer => writer.Write(id));
        }
    }

    /// <summary>Host: Altruist will bodyId wiederbeleben.</summary>
    public static void HostRevive(PlayerControl altruist, byte bodyId)
    {
        if (RoleState.Get(altruist.PlayerId) != RoleId.Altruist || !Players.IsAlive(altruist) || Players.InMeeting) return;
        var dead = Players.ById(bodyId);
        var body = Bodies.ById(bodyId);
        if (dead == null || dead.Data == null || !dead.Data.IsDead || dead.Data.Disconnected || body == null) return;
        if (Vector2.Distance(altruist.GetTruePosition(), body.TruePosition) > Bodies.Range + 1f) return;

        var position = body.TruePosition;
        var role = Deaths.RoleBeforeDeath(bodyId) ?? (Players.IsImpostor(dead) ? RoleTypes.Impostor : RoleTypes.Crewmate);
        Attack.Kill(altruist);
        Net.Send(RpcIds.Revive, writer =>
        {
            writer.Write(bodyId);
            writer.Write(position.x);
            writer.Write(position.y);
            writer.Write((ushort)role);
        });
        OnRevive(bodyId, position, role);
        dead.Data.MarkDirty();
    }

    /// <summary>Alle Clients (vom Host).</summary>
    public static void OnRevive(byte playerId, Vector2 position, RoleTypes role)
    {
        var player = Players.ById(playerId);
        if (player == null || player.Data == null || !player.Data.IsDead) return;
        Bodies.Remove(playerId);
        Deaths.Forget(playerId);
        player.Revive();
        RoleManager.Instance.SetRole(player, role);
        Attack.MarkChanged(playerId); // der Server hält ihn weiter für tot
        if (player.AmOwner)
        {
            player.NetTransform.RpcSnapTo(position); // bewegen darf sich jeder nur selbst
            HudManager.Instance?.SetHudActive(true);
            Notify.Show(Colors.Paint(RoleId.Altruist, "Ein Altruist hat dich wiederbelebt!"));
        }
        SpecialRolesPlugin.Logger.LogInfo($"Spieler {playerId} wiederbelebt.");
    }
}
