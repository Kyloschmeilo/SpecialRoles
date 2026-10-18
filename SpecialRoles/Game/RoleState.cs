using System.Collections.Generic;
using System.Linq;
using SpecialRoles.Core;

namespace SpecialRoles.Game;

/// <summary>Rollen der laufenden Runde (bei allen gleich, kommt vom Host). Angezeigt wird nur die eigene.</summary>
internal static class RoleState
{
    private static readonly Dictionary<byte, RoleId> Roles = new();
    private static readonly Dictionary<byte, Modifier> Modifiers = new();

    public static RoundSettings Settings { get; private set; } = RoundSettings.Default;

    public static RoleId Get(byte playerId) => Roles.TryGetValue(playerId, out var role) ? role : RoleId.None;

    public static bool Has(byte playerId, Modifier modifier) =>
        Modifiers.TryGetValue(playerId, out var mods) && (mods & modifier) != 0;

    public static RoleId Local => PlayerControl.LocalPlayer != null ? Get(PlayerControl.LocalPlayer.PlayerId) : RoleId.None;

    public static bool LocalHas(Modifier modifier) =>
        PlayerControl.LocalPlayer != null && Has(PlayerControl.LocalPlayer.PlayerId, modifier);

    public static void Apply(RoleSync sync)
    {
        Reset();
        Settings = sync.Settings;
        foreach (var a in sync.Assignments)
        {
            if (a.Role != RoleId.None) Roles[a.PlayerId] = a.Role;
            if (a.Modifiers != Modifier.None) Modifiers[a.PlayerId] = a.Modifiers;
        }
        var local = Local;
        SpecialRolesPlugin.Logger.LogInfo(
            $"Rollen erhalten: {Roles.Count} Rollen, {Modifiers.Count} Modifikatoren. Deine Rolle: {RoleCatalog.Get(local)?.Name ?? "keine"}" +
            (LocalHas(Modifier.Flash) ? " + Flash" : ""));
    }

    public static void Reset()
    {
        Roles.Clear();
        Modifiers.Clear();
        Settings = RoundSettings.Default;
    }

    public static IEnumerable<byte> PlayersWith(RoleId role) => Roles.Where(kv => kv.Value == role).Select(kv => kv.Key);
}
