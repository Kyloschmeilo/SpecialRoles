using System.Collections.Generic;
using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpecialRoles.Roles;

/// <summary>
/// Fallensteller (Crew): legt Fallen, die nur er sieht. Läuft jemand hinein, erfährt er den Namen; die Falle ist dann weg.
/// Läuft komplett beim Fallensteller (die Positionen aller Spieler kennt jeder Client).
/// </summary>
internal static class Trapper
{
    private const float TriggerRange = 0.6f;
    private const float PlaceCooldown = 10f;

    private static readonly List<SpriteRenderer> Traps = new();
    private static int _placed;

    public static readonly TrapButton Button = new();

    internal sealed class TrapButton : RoleButton
    {
        protected override string Label => $"Falle ({Left})";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => PlaceCooldown;
        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => Left > 0;

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            if (Left <= 0) return;
            _placed++;
            Traps.Add(Sprites.Spawn("Falle", Sprites.Ring, local.GetTruePosition(), 0.5f, Colors.Of(RoleId.Trapper, 0.7f)));
        }
    }

    private static int Left => RoleState.Settings.GetInt(SettingId.TrapperTraps) - _placed;

    public static void Update(PlayerControl local)
    {
        if (RoleState.Local != RoleId.Trapper)
        {
            Clear();
            return;
        }
        if (Round.JustTeleported) return; // Meeting: alle stehen am Tisch, das wäre kein echter Treffer
        for (var i = Traps.Count - 1; i >= 0; i--)
        {
            var trap = Traps[i];
            if (trap == null)
            {
                Traps.RemoveAt(i);
                continue;
            }
            var victim = Players.ClosestTo(trap.transform.position, TriggerRange, p => p.PlayerId != local.PlayerId);
            if (victim == null) continue;
            var name = Camouflager.Active ? "Jemand" : Players.ColoredName(victim.PlayerId);
            Notify.Show(name + Colors.Paint(RoleId.Trapper, " ist in deine Falle gelaufen!"), 8f);
            Object.Destroy(trap.gameObject);
            Traps.RemoveAt(i);
        }
    }

    private static void Clear()
    {
        foreach (var trap in Traps)
            if (trap != null) Object.Destroy(trap.gameObject);
        Traps.Clear();
    }

    public static void Reset()
    {
        Clear();
        _placed = 0;
    }
}
