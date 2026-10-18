using System.Collections.Generic;
using SpecialRoles.Core;
using SpecialRoles.Roles;

namespace SpecialRoles.Game;

/// <summary>Läuft jeden Frame (HudManager.Update): eigene Rolle anzeigen, Fähigkeits-Knöpfe, passive Effekte.</summary>
internal static class Hud
{
    // Eigene Klasse heißt RoleButton, weil das Spiel schon eine Klasse AbilityButton hat
    private static readonly Dictionary<RoleId, RoleButton> Buttons = new()
    {
        [RoleId.Sheriff] = new Sheriff.ShootButton(),
        [RoleId.Janitor] = new Janitor.CleanButton(),
    };

    public static void Tick()
    {
        var hud = HudManager.Instance;
        var local = PlayerControl.LocalPlayer;
        if (hud == null || local == null || local.Data == null) return;

        Safe.Run(() => NameTag.Update(local));
        Safe.Run(() => Flash.Update(local));
        if (Buttons.TryGetValue(RoleState.Local, out var button))
            Safe.Run(() => button.Tick(hud, local));
    }

    /// <summary>Neue Runde oder Spielende: Knöpfe weg, Tempo zurück.</summary>
    public static void Reset()
    {
        foreach (var button in Buttons.Values) Safe.Run(button.Reset);
        Safe.Run(Flash.Reset);
    }
}

/// <summary>Rolle unter dem eigenen Namen und oben in der Aufgabenliste – nur für einen selbst sichtbar.</summary>
internal static class NameTag
{
    private static bool _tagged;

    public static void Update(PlayerControl local)
    {
        var text = local.cosmetics?.nameText;
        if (text == null) return;
        var suffix = RoleLine();
        if (suffix.Length == 0 && !_tagged) return; // ohne Mod-Rolle den Namen dem Spiel überlassen
        var name = local.Data.PlayerName ?? "";
        text.text = suffix.Length == 0 ? name : $"{name}\n<size=70%>{suffix}</size>";
        _tagged = suffix.Length > 0;
    }

    /// <summary>„Sheriff + Flash“ in Rollenfarben, leer ohne Mod-Rolle.</summary>
    public static string RoleLine()
    {
        var parts = new List<string>();
        var info = RoleCatalog.Get(RoleState.Local);
        if (info != null) parts.Add($"<color=#{info.Color}>{info.Name}</color>");
        if (RoleState.LocalHas(Modifier.Flash)) parts.Add($"<color=#{RoleCatalog.FlashColor}>Flash</color>");
        return string.Join(" + ", parts);
    }

    /// <summary>Text, der vor die Aufgabenliste kommt.</summary>
    public static string TaskHeader()
    {
        var lines = new List<string>();
        var info = RoleCatalog.Get(RoleState.Local);
        if (info != null) lines.Add($"<color=#{info.Color}>{info.Name}: {info.Blurb}</color>");
        if (RoleState.LocalHas(Modifier.Flash)) lines.Add($"<color=#{RoleCatalog.FlashColor}>Flash: Du bist schneller als alle anderen.</color>");
        return lines.Count == 0 ? "" : string.Join("\n", lines) + "\n";
    }
}
