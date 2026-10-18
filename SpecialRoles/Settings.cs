using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using SpecialRoles.Core;

namespace SpecialRoles;

/// <summary>
/// Einstellungen in BepInEx/config/de.spielenetzwerk.specialroles.cfg. Es zählen die des Hosts: er verteilt die
/// Rollen und schickt Cooldown/Tempo an alle mit. Nur fertige Rollen haben Einträge.
/// </summary>
internal static class Settings
{
    private sealed record RoleEntry(RoleId Role, ConfigEntry<int> Count, ConfigEntry<int> Chance);

    private static readonly List<RoleEntry> Roles = new();
    private static ConfigEntry<int> _flashCount = null!;
    private static ConfigEntry<int> _flashChance = null!;
    private static ConfigEntry<float> _flashSpeed = null!;
    private static ConfigEntry<float> _sheriffCooldown = null!;

    public static void Bind(ConfigFile config)
    {
        foreach (var info in RoleCatalog.All.Where(r => r.Implemented))
        {
            var count = config.Bind("Rollen", $"{info.Name} Anzahl", 1,
                new ConfigDescription($"Wie viele {info.Name} es höchstens gibt (0 = aus).",
                    new AcceptableValueRange<int>(0, RoleAssigner.MaxCount)));
            var chance = config.Bind("Rollen", $"{info.Name} Chance", 100,
                new ConfigDescription($"Chance in Prozent, dass es einen {info.Name} gibt (100 = immer).",
                    new AcceptableValueRange<int>(0, 100)));
            Roles.Add(new RoleEntry(info.Id, count, chance));
        }

        _sheriffCooldown = config.Bind("Sheriff", "Cooldown", RoundSettings.Default.SheriffCooldown,
            new ConfigDescription("Sekunden zwischen zwei Schüssen.", new AcceptableValueRange<float>(5f, 120f)));

        _flashCount = config.Bind("Modifikatoren", "Flash Anzahl", 1,
            new ConfigDescription("Wie viele Spieler höchstens Flash sind (zusätzlich zu ihrer Rolle, 0 = aus).",
                new AcceptableValueRange<int>(0, RoleAssigner.MaxCount)));
        _flashChance = config.Bind("Modifikatoren", "Flash Chance", 100,
            new ConfigDescription("Chance in Prozent, dass es einen Flash gibt.", new AcceptableValueRange<int>(0, 100)));
        _flashSpeed = config.Bind("Modifikatoren", "Flash Tempo", RoundSettings.Default.FlashSpeed,
            new ConfigDescription("Lauftempo von Flash (1.25 = 25 % schneller).", new AcceptableValueRange<float>(1f, 2f)));
    }

    public static IEnumerable<RoleSetting> RoleSettings() =>
        Roles.Select(r => new RoleSetting(r.Role, r.Count.Value, r.Chance.Value));

    public static int FlashCount => _flashCount.Value;
    public static int FlashChance => _flashChance.Value;

    public static RoundSettings Round() => new(_sheriffCooldown.Value, _flashSpeed.Value);
}
