using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Transporter (Crew): markiert einen Spieler, geht zu einem zweiten und tauscht die beiden. Jeder Getauschte versetzt
/// sich auf seinem eigenen Client (bewegen darf sich jeder nur selbst), die Zielpositionen kommen vom Transporter.
/// </summary>
internal static class Transporter
{
    private static byte? _marked;
    private static int _used;

    public static readonly TransportButton Button = new();

    internal sealed class TransportButton : RoleButton
    {
        protected override string Label => _marked == null ? $"Markieren ({Left})" : "Tauschen";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => RoleState.Settings.Get(SettingId.TransporterCooldown);
        protected override bool CooldownAfterUse => false;

        protected override PlayerControl? FindTarget(PlayerControl local)
        {
            if (Left <= 0) return null;
            if (_marked != null && !Players.IsAlive(Players.ById(_marked.Value))) _marked = null;
            return Players.ClosestTarget(local, Players.KillDistance(), p => p.PlayerId != _marked && CanMove(p));
        }

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            if (target == null) return;
            if (_marked == null)
            {
                _marked = target.PlayerId;
                Notify.Show(Players.ColoredName(target.PlayerId) + Colors.Paint(RoleId.Transporter, " markiert – jetzt zum zweiten Spieler gehen."));
                return;
            }
            var first = Players.ById(_marked.Value);
            _marked = null;
            if (first == null || !Players.IsAlive(first) || !CanMove(first)) return;
            _used++;
            SetTimer(Cooldown);

            var a = first.PlayerId;
            var b = target.PlayerId;
            var posA = first.GetTruePosition();
            var posB = target.GetTruePosition();
            Net.Send(RpcIds.Transport, writer =>
            {
                writer.Write(a);
                writer.Write(b);
                writer.Write(posA.x);
                writer.Write(posA.y);
                writer.Write(posB.x);
                writer.Write(posB.y);
            });
            OnTransport(local, a, b, posA, posB);
        }
    }

    private static int Left => RoleState.Settings.GetInt(SettingId.TransporterUses) - _used;

    private static bool CanMove(PlayerControl player) => !player.inVent && !player.onLadder && !player.inMovingPlat;

    /// <summary>Alle Clients: wer selbst getauscht wird, versetzt sich.</summary>
    public static void OnTransport(PlayerControl sender, byte a, byte b, Vector2 posA, Vector2 posB)
    {
        if (RoleState.Get(sender.PlayerId) != RoleId.Transporter || !Players.IsAlive(sender)) return;
        var local = PlayerControl.LocalPlayer;
        if (local == null || !Players.IsAlive(local) || !CanMove(local)) return;
        Vector2 destination;
        if (local.PlayerId == a) destination = posB;
        else if (local.PlayerId == b) destination = posA;
        else return;

        if (Minigame.Instance != null) Minigame.Instance.Close();
        local.NetTransform.RpcSnapTo(destination);
        Notify.Show(Colors.Paint(RoleId.Transporter, "Ein Transporter hat dich versetzt!"));
    }

    public static void OnMeetingEnd() => _marked = null;

    public static void Reset()
    {
        _marked = null;
        _used = 0;
    }
}
