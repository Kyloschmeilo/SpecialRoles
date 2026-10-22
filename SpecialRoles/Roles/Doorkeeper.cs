using System.Collections.Generic;
using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Türwächter (Crew): schließt die Türen aller Räume in seiner Nähe (wie die Tür-Sabotage, nur für einen Raum
/// rundherum). Der Türwächter schickt die Räume, der Host schließt sie über das Spiel.
/// </summary>
internal static class Doorkeeper
{
    private const float Range = 6f;

    public static readonly DoorButton Button = new();

    internal sealed class DoorButton : RoleButton
    {
        protected override string Label => "Türen";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => RoleState.Settings.Get(SettingId.DoorkeeperCooldown);
        protected override bool Visible(PlayerControl local) => Players.IsAlive(local) && HasDoors();
        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => RoomsNear(local).Count > 0;

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            var rooms = RoomsNear(local);
            if (rooms.Count == 0) return;
            if (Players.IsHost) HostClose(local, rooms);
            else
                Net.Send(RpcIds.CloseDoors, writer =>
                {
                    writer.Write((byte)rooms.Count);
                    foreach (var room in rooms) writer.Write((byte)room);
                });
            Notify.Show(Colors.Paint(RoleId.Doorkeeper, "Türen geschlossen."));
        }
    }

    private static bool HasDoors()
    {
        var ship = ShipStatus.Instance;
        return ship != null && ship.AllDoors != null && ship.AllDoors.Length > 0;
    }

    private static List<SystemTypes> RoomsNear(PlayerControl local)
    {
        var rooms = new List<SystemTypes>();
        var ship = ShipStatus.Instance;
        if (ship == null || ship.AllDoors == null) return rooms;
        var position = local.GetTruePosition();
        foreach (var door in ship.AllDoors)
        {
            if (door == null || rooms.Contains(door.Room)) continue;
            if (Vector2.Distance(position, door.transform.position) <= Range) rooms.Add(door.Room);
        }
        return rooms;
    }

    /// <summary>Host: Türen der Räume schließen.</summary>
    public static void HostClose(PlayerControl doorkeeper, List<SystemTypes> rooms)
    {
        if (RoleState.Get(doorkeeper.PlayerId) != RoleId.Doorkeeper || !Players.IsAlive(doorkeeper)) return;
        var ship = ShipStatus.Instance;
        if (ship == null) return;
        foreach (var room in rooms) ship.RpcCloseDoorsOfType(room);
    }
}
