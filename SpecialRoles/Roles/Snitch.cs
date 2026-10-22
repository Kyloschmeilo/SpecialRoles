using System.Collections.Generic;
using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Snitch (Crew): hat er alle Tasks erledigt, zeigen Pfeile auf die lebenden Impostor. Fehlt ihm nur noch ein Task,
/// bekommen die Impostor einen Pfeil auf ihn (Tasks sieht jeder Client, die Spieldaten gehen an alle).
/// </summary>
internal static class Snitch
{
    private static readonly HashSet<string> Told = new();

    public static void Update(PlayerControl local)
    {
        if (RoleState.Local == RoleId.Snitch && Players.IsAlive(local) && TasksLeft(local) == 0)
        {
            Once("done", "Alle Tasks erledigt – die Pfeile zeigen auf die Impostor!");
            foreach (var player in Players.Alive())
                if (player.PlayerId != local.PlayerId && Players.IsImpostor(player))
                    Arrows.Point($"snitch:{player.PlayerId}", local, player.GetTruePosition(), Color.red);
        }

        if (Players.IsImpostor(local) && Players.IsAlive(local))
        {
            foreach (var id in RoleState.PlayersWith(RoleId.Snitch))
            {
                var snitch = Players.ById(id);
                if (!Players.IsAlive(snitch) || TasksLeft(snitch!) > 1) continue;
                Once($"warn:{id}", "Ein Snitch ist fast fertig – der Pfeil zeigt auf ihn!", RoleId.Snitch);
                Arrows.Point($"snitch-warn:{id}", local, snitch!.GetTruePosition(), Colors.Of(RoleId.Snitch));
            }
        }
    }

    /// <summary>Offene Tasks; ohne Tasks (Einstellung 0) zählt der Snitch nie als fertig.</summary>
    private static int TasksLeft(PlayerControl player)
    {
        var tasks = player.Data?.Tasks;
        if (tasks == null || tasks.Count == 0) return int.MaxValue;
        var left = 0;
        foreach (var task in tasks)
            if (task != null && !task.Complete)
                left++;
        return left;
    }

    private static void Once(string key, string text, RoleId color = RoleId.Snitch)
    {
        if (Told.Add(key)) Notify.Show(Colors.Paint(color, text));
    }

    public static void Reset() => Told.Clear();
}
