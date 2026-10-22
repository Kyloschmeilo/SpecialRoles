using System.Collections.Generic;
using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Sheriff (Crew): schießt auf einen Spieler in Kill-Reichweite. Impostor → der stirbt, sonst stirbt der Sheriff.
/// Der Sheriff schickt nur den Wunsch; der Host prüft und führt den Kill über das Spiel aus (wie ein normaler Kill).
/// </summary>
internal static class Sheriff
{
    // Host: letzter Schuss je Sheriff, gegen zu schnelles Feuern (z.B. andere Mod-Version)
    private static readonly Dictionary<byte, float> LastShot = new();

    public static readonly ShootButton Button = new();

    internal sealed class ShootButton : RoleButton
    {
        protected override string Label => "Schießen";
        protected override KeyCode Key => KeyCode.Q;
        protected override float Cooldown => RoleState.Settings.SheriffCooldown;

        protected override PlayerControl? FindTarget(PlayerControl local) => Players.ClosestTarget(local, Players.KillDistance());

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            if (target == null) return;
            if (AmongUsClient.Instance.AmHost)
                HostResolve(local, target.PlayerId);
            else
                Net.Send(RpcIds.SheriffShoot, writer => writer.Write(target.PlayerId));
        }
    }

    public static void HostResolve(PlayerControl sheriff, byte targetId)
    {
        if (RoleState.Get(sheriff.PlayerId) != RoleId.Sheriff || !Players.IsAlive(sheriff)) return;
        var target = Players.ById(targetId);
        if (target == null || !Players.IsAlive(target) || target.PlayerId == sheriff.PlayerId) return;

        var now = Time.time;
        if (LastShot.TryGetValue(sheriff.PlayerId, out var last) && now - last < RoleState.Settings.SheriffCooldown - 2f)
        {
            SpecialRolesPlugin.Logger.LogWarning("Sheriff hat zu schnell geschossen – ignoriert.");
            return;
        }
        LastShot[sheriff.PlayerId] = now;

        if (!Attack.Contact(sheriff, target)) return; // wachsamer Veteran: der Sheriff stirbt
        if (!Players.IsImpostor(target))
            Attack.Kill(sheriff); // daneben: der Sheriff stirbt selbst
        else if (Attack.Lethal(target))
            sheriff.RpcMurderPlayer(target, true);
    }

    public static void Reset() => LastShot.Clear();
}
