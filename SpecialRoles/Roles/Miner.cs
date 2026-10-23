using System.Collections.Generic;
using System.Linq;
using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpecialRoles.Roles;

/// <summary>
/// Maulwurf (Impostor): baut eigene Vents, die untereinander verbunden sind (wie eine Vent-Reihe im Spiel). Jeder
/// Client baut den Vent als Kopie eines vorhandenen mit derselben Id, damit das Venten des Spiels ihn findet.
/// Diese Ids kennt der Server nicht: Ein-/Aussteigen und die Ventilation laufen darum an ihm vorbei (Patches.cs),
/// sonst fliegt der Maulwurf wegen „Hacking“ raus (am 23.10.2026 passiert).
/// </summary>
internal static class Miner
{
    private const int FirstId = 150;
    private const int MaxPerMiner = 6;
    private const float PlaceCooldown = 10f;

    // Alle Clients: Vents je Maulwurf in Bau-Reihenfolge
    private static readonly Dictionary<byte, List<Vent>> Built = new();

    public static readonly VentButton Button = new();

    internal sealed class VentButton : RoleButton
    {
        protected override string Label => $"Vent ({Left(PlayerControl.LocalPlayer)})";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => PlaceCooldown;
        protected override bool Visible(PlayerControl local) => Players.IsAlive(local) && Left(local) > 0;
        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => !TooClose(local.GetTruePosition());

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            var count = Count(local.PlayerId);
            if (count >= Max) return;
            var id = FirstId + local.PlayerId * MaxPerMiner + count;
            var position = local.GetTruePosition();
            Net.Send(RpcIds.MinerVent, writer =>
            {
                writer.Write(id);
                writer.Write(position.x);
                writer.Write(position.y);
            });
            OnVent(local, id, position);
        }
    }

    private static int Max => System.Math.Min(RoleState.Settings.GetInt(SettingId.MinerVents), MaxPerMiner);

    private static int Count(byte miner) => Built.TryGetValue(miner, out var list) ? list.Count : 0;

    private static int Left(PlayerControl? local) => local == null ? 0 : Max - Count(local.PlayerId);

    private static bool TooClose(Vector2 position)
    {
        var ship = ShipStatus.Instance;
        if (ship == null || ship.AllVents == null) return true;
        foreach (var vent in ship.AllVents)
            if (vent != null && Vector2.Distance(position, vent.transform.position) < 1f)
                return true;
        return false;
    }

    public static void OnVent(PlayerControl miner, int id, Vector2 position)
    {
        if (RoleState.Get(miner.PlayerId) != RoleId.Miner || !Players.IsAlive(miner)) return;
        var ship = ShipStatus.Instance;
        if (ship == null || ship.AllVents == null || ship.AllVents.Length == 0) return;
        if (ship.AllVents.Any(v => v != null && v.Id == id)) return; // schon gebaut

        var template = ship.AllVents[0];
        var vent = Object.Instantiate(template, template.transform.parent);
        vent.name = "SpecialRoles_Vent_" + id;
        vent.Id = id;
        vent.transform.position = new Vector3(position.x, position.y, position.y / 1000f + 0.001f);
        vent.Left = null;
        vent.Right = null;
        vent.Center = null;

        if (!Built.TryGetValue(miner.PlayerId, out var list)) Built[miner.PlayerId] = list = new List<Vent>();
        if (list.Count > 0)
        {
            var previous = list[^1];
            previous.Right = vent;
            vent.Left = previous;
        }
        list.Add(vent);

        var all = ship.AllVents.ToList();
        all.Add(vent);
        ship.AllVents = all.ToArray();
        SpecialRolesPlugin.Logger.LogInfo($"Maulwurf-Vent {id} gebaut.");
    }

    /// <summary>Gehört die Vent-Id einem Maulwurf-Vent? (Die Vents des Spiels haben kleine Ids.)</summary>
    public static bool IsOurs(int ventId) => ventId >= FirstId;

    /// <summary>
    /// RpcEnterVent/RpcExitVent-Prefix: der eigene Spieler benutzt einen Maulwurf-Vent → selbst ausführen und über die
    /// eigene Nachricht an alle. true = erledigt, das Spiel schickt nichts.
    /// </summary>
    public static bool TryUseVent(PlayerPhysics physics, int ventId, bool enter)
    {
        var player = physics.myPlayer;
        if (!IsOurs(ventId) || player == null || !player.AmOwner) return false;
        Net.Send(RpcIds.MinerVentUse, writer =>
        {
            writer.Write(ventId);
            writer.Write(enter);
        });
        OnUseVent(player, ventId, enter);
        return true;
    }

    /// <summary>Alle Clients: dieselbe Animation wie beim Venten des Spiels.</summary>
    public static void OnUseVent(PlayerControl player, int ventId, bool enter)
    {
        var physics = player.MyPhysics;
        if (!IsOurs(ventId) || physics == null) return;
        physics.StartCoroutine(enter ? physics.CoEnterVent(ventId) : physics.CoExitVent(ventId));
    }

    /// <summary>Spielende: die Vents gehören zum Schiff und verschwinden mit ihm.</summary>
    public static void Reset() => Built.Clear();
}
