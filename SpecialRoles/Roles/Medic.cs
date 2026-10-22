using System.Collections.Generic;
using System.Linq;
using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Medic (Crew): gibt einem Spieler einen Schild, der den nächsten tödlichen Angriff abwehrt. Immer nur ein Schild;
/// ist er gebrochen, geht nach dem Cooldown ein neuer. Den Ring sehen nur der Medic und der Geschützte.
/// </summary>
internal static class Medic
{
    // Alle Clients: Medic → geschützter Spieler
    private static readonly Dictionary<byte, byte> Shields = new();

    public static readonly ShieldButton Button = new();

    internal sealed class ShieldButton : RoleButton
    {
        protected override string Label => "Schild";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => RoleState.Settings.Get(SettingId.MedicCooldown);

        protected override PlayerControl? FindTarget(PlayerControl local) =>
            Shields.ContainsKey(local.PlayerId) ? null : Players.ClosestTarget(local, Players.KillDistance());

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            if (target == null) return;
            var id = target.PlayerId;
            Net.Send(RpcIds.MedicShield, writer => writer.Write(id));
            OnShield(local, id);
        }
    }

    public static void OnShield(PlayerControl medic, byte targetId)
    {
        if (RoleState.Get(medic.PlayerId) != RoleId.Medic || !Players.IsAlive(medic)) return;
        Shields[medic.PlayerId] = targetId;
        var local = PlayerControl.LocalPlayer;
        if (local == null) return;
        if (local.PlayerId == medic.PlayerId)
            Notify.Show($"{Players.ColoredName(targetId)} hat jetzt deinen Schild.");
        else if (local.PlayerId == targetId)
            Notify.Show(Colors.Paint(RoleId.Medic, "Ein Medic hat dir einen Schild gegeben."));
    }

    /// <summary>Host: ist targetId geschützt, bricht der Schild und der Angriff fällt aus.</summary>
    public static bool TryBlock(byte targetId)
    {
        if (!Shields.ContainsValue(targetId)) return false;
        Attack.SendProtected(targetId, Protection.MedicShield);
        return true;
    }

    /// <summary>Alle Clients: der Schild von targetId hat einen Angriff abgewehrt.</summary>
    public static void OnBroken(byte targetId)
    {
        var medics = Shields.Where(kv => kv.Value == targetId).Select(kv => kv.Key).ToList();
        foreach (var medic in medics) Shields.Remove(medic);
        var local = PlayerControl.LocalPlayer;
        if (local == null) return;
        if (medics.Contains(local.PlayerId))
        {
            Notify.Show(Colors.Paint(RoleId.Medic, "Jemand hat ") + Players.ColoredName(targetId) + Colors.Paint(RoleId.Medic, " angegriffen – dein Schild ist gebrochen!"));
            Button.SetTimer(RoleState.Settings.Get(SettingId.MedicCooldown));
        }
        if (local.PlayerId == targetId)
            Notify.Show(Colors.Paint(RoleId.Medic, "Dein Schild hat dich gerettet!"));
    }

    /// <summary>Jeden Frame: Ring um den Geschützten (für Medic und Geschützten).</summary>
    public static void Update(PlayerControl local)
    {
        foreach (var (medic, target) in Shields)
        {
            if (local.PlayerId != medic && local.PlayerId != target) continue;
            var player = Players.ById(target);
            if (Players.IsAlive(player))
                Markers.Ring($"medic:{medic}", player!, Colors.Of(RoleId.Medic, 0.8f));
        }
    }

    public static void Reset() => Shields.Clear();
}
