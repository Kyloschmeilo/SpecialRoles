using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using SpecialRoles.Core;

namespace SpecialRoles;

/// <summary>
/// Einstellungen in BepInEx/config/de.spielenetzwerk.specialroles.cfg. Es zählen die des Hosts: er verteilt die
/// Rollen und schickt die Werte aus <see cref="SettingCatalog"/> an alle mit. Nur fertige Rollen haben Einträge.
/// </summary>
internal static class Settings
{
    private sealed record RoleEntry(RoleId Role, ConfigEntry<int> Count, ConfigEntry<int> Chance);

    private static readonly List<RoleEntry> Roles = new();
    private static readonly Dictionary<SettingId, ConfigEntry<float>> Values = new();
    private static ConfigEntry<int> _flashCount = null!;
    private static ConfigEntry<int> _flashChance = null!;
    private static ConfigEntry<int> _gameRoleShare = null!;

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

        _flashCount = config.Bind("Modifikatoren", "Flash Anzahl", 1,
            new ConfigDescription("Wie viele Spieler höchstens Flash sind (zusätzlich zu ihrer Rolle, 0 = aus).",
                new AcceptableValueRange<int>(0, RoleAssigner.MaxCount)));
        _flashChance = config.Bind("Modifikatoren", "Flash Chance", 100,
            new ConfigDescription("Chance in Prozent, dass es einen Flash gibt.", new AcceptableValueRange<int>(0, 100)));

        _gameRoleShare = config.Bind("Vergabe", "Spiel-Rollen Anteil", 50,
            new ConfigDescription("Spiel-Rollen (Engineer, Shapeshifter, Viper …) kommen beim Rundenstart nur mit diesem " +
                "Anteil ihrer Lobby-Chance vor (in Prozent), damit Mod-Rollen häufiger sind. 100 = wie in der Lobby eingestellt.",
                new AcceptableValueRange<int>(0, 100)));

        foreach (var def in SettingCatalog.All)
        {
            if (def.Role != RoleId.None && RoleCatalog.Get(def.Role)?.Implemented != true) continue;
            Values[def.Id] = config.Bind(def.Section, def.Key, def.Default,
                new ConfigDescription(def.Description, new AcceptableValueRange<float>(def.Min, def.Max)));
        }
    }

    public static IEnumerable<RoleSetting> RoleSettings() =>
        Roles.Select(r => new RoleSetting(r.Role, r.Count.Value, r.Chance.Value));

    public static int FlashCount => _flashCount.Value;
    public static int FlashChance => _flashChance.Value;
    public static int GameRoleShare => _gameRoleShare.Value;

    public static RoundSettings Round() =>
        new(Values.Select(kv => new KeyValuePair<SettingId, float>(kv.Key, kv.Value.Value)));
}
