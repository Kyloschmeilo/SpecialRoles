using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Mechaniker (Crew): repariert einmal pro Runde (bis zum nächsten Meeting) alle Sabotagen von überall.
/// Reparieren macht der Host direkt an den Schiffssystemen (die gehen dann an alle).
/// </summary>
internal static class Mechanic
{
    private static bool _used;

    public static readonly RepairButton Button = new();

    internal sealed class RepairButton : RoleButton
    {
        protected override string Label => "Reparieren";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => 0f;
        protected override bool Visible(PlayerControl local) => Players.IsAlive(local) && !_used;
        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => SabotageActive(local);

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            _used = true;
            if (Players.IsHost) HostRepair(local);
            else Net.Send(RpcIds.MechanicFix, _ => { });
            Notify.Show(Colors.Paint(RoleId.Mechanic, "Alle Sabotagen repariert."));
        }
    }

    private static bool SabotageActive(PlayerControl local)
    {
        if (local.myTasks == null) return false;
        foreach (var task in local.myTasks)
            if (task != null && PlayerTask.TaskIsEmergency(task))
                return true;
        return false;
    }

    /// <summary>Host: alle Sabotagen beenden.</summary>
    public static void HostRepair(PlayerControl mechanic)
    {
        if (RoleState.Get(mechanic.PlayerId) != RoleId.Mechanic || !Players.IsAlive(mechanic)) return;
        var ship = ShipStatus.Instance;
        if (ship == null) return;

        ship.RepairCriticalSabotages(); // Reaktor, Sauerstoff, Labor, Absturz (Airship)
        var systems = ship.Systems;
        if (systems.ContainsKey(SystemTypes.Electrical))
        {
            var lights = systems[SystemTypes.Electrical].TryCast<SwitchSystem>();
            if (lights != null && lights.ActualSwitches != lights.ExpectedSwitches)
            {
                lights.ActualSwitches = lights.ExpectedSwitches;
                lights.IsDirty = true;
            }
        }
        if (systems.ContainsKey(SystemTypes.Comms))
        {
            var comms = systems[SystemTypes.Comms];
            var hud = comms.TryCast<HudOverrideSystemType>();
            if (hud != null && hud.IsActive)
            {
                ship.UpdateSystem(SystemTypes.Comms, mechanic, (byte)0);
            }
            else if (comms.TryCast<HqHudSystemType>() != null)
            {
                // MIRA: beide Konsolen als repariert melden
                ship.UpdateSystem(SystemTypes.Comms, mechanic, (byte)(16 | 0));
                ship.UpdateSystem(SystemTypes.Comms, mechanic, (byte)(16 | 1));
            }
        }
        SpecialRolesPlugin.Logger.LogInfo("Mechaniker hat repariert.");
    }

    /// <summary>Nach jedem Meeting wieder verfügbar.</summary>
    public static void OnMeetingEnd() => _used = false;

    public static void Reset() => _used = false;
}
