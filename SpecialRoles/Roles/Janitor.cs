using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>Putzkraft (Impostor): lässt eine Leiche in Reichweite verschwinden; teilt sich den Cooldown mit dem Kill.</summary>
internal static class Janitor
{
    public static readonly CleanButton Button = new();

    internal sealed class CleanButton : RoleButton
    {
        protected override string Label => "Putzen";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => Players.KillCooldown();
        protected override bool SharesKillTimer => true;

        protected override Sprite? Icon(HudManager hud) => hud.ReportButton != null ? hud.ReportButton.graphic.sprite : null;

        protected override PlayerControl? FindTarget(PlayerControl local) => null; // Ziel ist eine Leiche, kein Spieler

        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => ClosestBody(local) != null;

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            var body = ClosestBody(local);
            if (body == null) return;
            var id = body.ParentId;
            Net.Send(RpcIds.CleanBody, writer => writer.Write(id));
            RemoveBody(id);
            local.SetKillTimer(Players.KillCooldown());
        }
    }

    private static DeadBody? ClosestBody(PlayerControl local) => Bodies.Closest(local.GetTruePosition());

    public static void OnCleanReceived(PlayerControl sender, byte bodyId)
    {
        if (RoleState.Get(sender.PlayerId) != RoleId.Janitor || !Players.IsAlive(sender)) return;
        RemoveBody(bodyId);
    }

    private static void RemoveBody(byte bodyId) => Bodies.Remove(bodyId);
}
