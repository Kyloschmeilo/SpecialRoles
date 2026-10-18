using System;
using System.Collections.Generic;
using System.Linq;

namespace SpecialRoles.Core;

/// <param name="CanGetRole">false = hat schon eine Spielrolle (Engineer, Shapeshifter …) und bekommt keine Mod-Rolle.</param>
public sealed record PlayerSlot(byte PlayerId, bool IsImpostor, bool CanGetRole);

/// <param name="Count">Wie oft die Rolle höchstens vorkommt (0 = aus).</param>
/// <param name="Chance">Chance in Prozent pro Exemplar; 100 = sicher dabei (solange Plätze frei sind).</param>
public sealed record RoleSetting(RoleId Role, int Count, int Chance);

public sealed record Assignment(byte PlayerId, RoleId Role, Modifier Modifiers);

/// <summary>
/// Verteilt die Mod-Rollen (nur beim Host). Fair: jeder Spieler hat dieselbe Chance auf jede Rolle seines Teams.
/// Crewmates bekommen Crew- oder Neutral-Rollen, Impostor nur Impostor-Rollen. Sichere Rollen (100 %) kommen zuerst
/// dran, die übrigen in zufälliger Reihenfolge; gibt es mehr Rollen als Plätze, fallen die letzten weg.
/// </summary>
public static class RoleAssigner
{
    public const int MaxCount = 15;

    /// <param name="next">Zufallszahl in [0, n) – im Spiel echter Zufall (RandomNumberGenerator), in Tests fest.</param>
    public static List<Assignment> Assign(
        IReadOnlyList<PlayerSlot> players,
        IEnumerable<RoleSetting> roles,
        int flashCount,
        int flashChance,
        Func<int, int> next)
    {
        var settings = roles
            .Where(s => RoleCatalog.Get(s.Role) != null)
            .Select(s => s with { Count = Math.Clamp(s.Count, 0, MaxCount), Chance = Math.Clamp(s.Chance, 0, 100) })
            .ToList();

        var result = new Dictionary<byte, RoleId>();
        AssignPool(players.Where(p => p.CanGetRole && !p.IsImpostor).ToList(),
            settings.Where(s => RoleCatalog.Get(s.Role)!.Team != RoleTeam.Impostor), next, result);
        AssignPool(players.Where(p => p.CanGetRole && p.IsImpostor).ToList(),
            settings.Where(s => RoleCatalog.Get(s.Role)!.Team == RoleTeam.Impostor), next, result);

        // Flash kann jeder bekommen, auch Spieler mit einer Spielrolle
        var flash = new HashSet<byte>();
        var flashCandidates = players.Select(p => p.PlayerId).Distinct().ToList();
        foreach (var _ in Rolls(Math.Clamp(flashCount, 0, MaxCount), Math.Clamp(flashChance, 0, 100), next))
        {
            if (flashCandidates.Count == 0) break;
            var index = next(flashCandidates.Count);
            flash.Add(flashCandidates[index]);
            flashCandidates.RemoveAt(index);
        }

        return players
            .Select(p => p.PlayerId)
            .Distinct()
            .Select(id => new Assignment(
                id,
                result.TryGetValue(id, out var role) ? role : RoleId.None,
                flash.Contains(id) ? Modifier.Flash : Modifier.None))
            .Where(a => a.Role != RoleId.None || a.Modifiers != Modifier.None)
            .ToList();
    }

    private static void AssignPool(List<PlayerSlot> pool, IEnumerable<RoleSetting> settings, Func<int, int> next, Dictionary<byte, RoleId> result)
    {
        var sure = new List<RoleId>();
        var maybe = new List<RoleId>();
        foreach (var setting in settings)
        {
            if (setting.Chance >= 100)
                sure.AddRange(Enumerable.Repeat(setting.Role, setting.Count));
            else
                maybe.AddRange(Rolls(setting.Count, setting.Chance, next).Select(_ => setting.Role));
        }
        Shuffle(sure, next);
        Shuffle(maybe, next);
        Shuffle(pool, next);

        var roles = sure.Concat(maybe).ToList();
        for (var i = 0; i < pool.Count && i < roles.Count; i++)
            result[pool[i].PlayerId] = roles[i];
    }

    /// <summary>Würfelt count-mal mit chance % und liefert die Treffer.</summary>
    private static IEnumerable<int> Rolls(int count, int chance, Func<int, int> next)
    {
        for (var i = 0; i < count; i++)
        {
            if (chance <= 0) yield break;
            if (chance >= 100 || next(100) < chance) yield return i;
        }
    }

    public static void Shuffle<T>(IList<T> list, Func<int, int> next)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
