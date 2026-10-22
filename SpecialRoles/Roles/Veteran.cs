using System.Collections.Generic;
using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>Veteran (Crew): wird für ein paar Sekunden wachsam – wer ihn dann angreift, stirbt selbst (siehe <see cref="Attack"/>).</summary>
internal static class Veteran
{
    // Alle Clients: wachsam bis Time.time (der Host entscheidet damit über Angriffe)
    private static readonly Dictionary<byte, float> AlertUntil = new();
    private static int _usesLeft = -1;

    public static readonly AlertButton Button = new();

    internal sealed class AlertButton : RoleButton
    {
        protected override string Label => $"Wachsam ({UsesLeft})";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => RoleState.Settings.Get(SettingId.VeteranCooldown);
        protected override PlayerControl? FindTarget(PlayerControl local) => null;
        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => UsesLeft > 0 && !IsAlert(local.PlayerId);

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            if (UsesLeft <= 0) return;
            _usesLeft = UsesLeft - 1;
            Net.Send(RpcIds.VeteranAlert, _ => { });
            OnAlert(local);
            Notify.Show(Colors.Paint(RoleId.Veteran, "Du bist wachsam – wer dich jetzt angreift, stirbt!"));
        }
    }

    private static int UsesLeft => _usesLeft < 0 ? RoleState.Settings.GetInt(SettingId.VeteranUses) : _usesLeft;

    public static void OnAlert(PlayerControl sender)
    {
        if (RoleState.Get(sender.PlayerId) != RoleId.Veteran || !Players.IsAlive(sender)) return;
        AlertUntil[sender.PlayerId] = Time.time + RoleState.Settings.Get(SettingId.VeteranDuration);
    }

    public static bool IsAlert(byte playerId) =>
        RoleState.Get(playerId) == RoleId.Veteran && AlertUntil.TryGetValue(playerId, out var until) && Time.time < until;

    public static void Reset()
    {
        AlertUntil.Clear();
        _usesLeft = -1;
    }
}
