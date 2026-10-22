using System.Collections.Generic;
using SpecialRoles.Core;
using SpecialRoles.Roles;

namespace SpecialRoles.Game;

/// <summary>Fähigkeits-Knöpfe je Rolle und die eigene Rolle unter dem Namen.</summary>
internal static class Hud
{
    // Eigene Klasse heißt RoleButton, weil das Spiel schon eine Klasse AbilityButton hat
    private static readonly Dictionary<RoleId, RoleButton[]> Buttons = new()
    {
        [RoleId.Sheriff] = new RoleButton[] { Sheriff.Button },
        [RoleId.Medic] = new RoleButton[] { Medic.Button },
        [RoleId.Hacker] = new RoleButton[] { Hacker.Button },
        [RoleId.Tracker] = new RoleButton[] { Tracker.Button },
        [RoleId.Trapper] = new RoleButton[] { Trapper.Button },
        [RoleId.Medium] = new RoleButton[] { Medium.Button },
        [RoleId.Veteran] = new RoleButton[] { Veteran.Button },
        [RoleId.Altruist] = new RoleButton[] { Altruist.Button },
        [RoleId.Mechanic] = new RoleButton[] { Mechanic.Button },
        [RoleId.Transporter] = new RoleButton[] { Transporter.Button },
        [RoleId.Oracle] = new RoleButton[] { Oracle.Button },
        [RoleId.Cameraman] = new RoleButton[] { Cameraman.Place, Cameraman.View },
        [RoleId.Doorkeeper] = new RoleButton[] { Doorkeeper.Button },
        [RoleId.Janitor] = new RoleButton[] { Janitor.Button },
        [RoleId.Undertaker] = new RoleButton[] { Undertaker.Button },
        [RoleId.Bomber] = new RoleButton[] { Bomber.Button },
        [RoleId.Grenadier] = new RoleButton[] { Grenadier.Button },
        [RoleId.Miner] = new RoleButton[] { Miner.Button },
        [RoleId.Blackmailer] = new RoleButton[] { Blackmailer.Button },
        [RoleId.Camouflager] = new RoleButton[] { Camouflager.Button },
        [RoleId.Warlock] = new RoleButton[] { Warlock.Button },
        [RoleId.Amnesiac] = new RoleButton[] { Amnesiac.Button },
    };

    private static RoleId _shownRole;

    /// <summary>Jeden Frame: Knöpfe der eigenen Rolle; bei Rollenwechsel (Vergesslicher) die alten verstecken.</summary>
    public static void TickButtons(HudManager hud, PlayerControl local)
    {
        var role = RoleState.Local;
        if (role != _shownRole)
        {
            if (Buttons.TryGetValue(_shownRole, out var old))
                foreach (var button in old)
                    button.Hide();
            _shownRole = role;
        }
        if (!Buttons.TryGetValue(role, out var buttons)) return;
        foreach (var button in buttons)
            Safe.Run(() => button.Tick(hud, local));
    }

    /// <summary>Neue Runde oder Spielende: Knöpfe weg.</summary>
    public static void Reset()
    {
        foreach (var buttons in Buttons.Values)
        foreach (var button in buttons)
            Safe.Run(button.Reset);
        _shownRole = RoleId.None;
    }
}

/// <summary>Rolle unter dem eigenen Namen und oben in der Aufgabenliste – nur für einen selbst sichtbar.</summary>
internal static class NameTag
{
    private static bool _tagged;

    public static void Update(PlayerControl local)
    {
        var text = local.cosmetics?.nameText;
        if (text == null || Camouflager.Active) return; // getarnt: kein Name, auch nicht der eigene
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

/// <summary>Im Meeting: „erpresst“ bzw. das vom Orakel verratene Team hinter dem Namen (für alle sichtbar).</summary>
internal static class MeetingTags
{
    public static void Update()
    {
        var meeting = MeetingHud.Instance;
        if (meeting == null || meeting.playerStates == null) return;
        foreach (var area in meeting.playerStates)
        {
            if (area == null || area.NameText == null) continue;
            var id = area.PlayerId.Value;
            var tags = new List<string>();
            var blackmail = Blackmailer.MeetingTag(id);
            if (blackmail.Length > 0) tags.Add(blackmail);
            var oracle = Oracle.MeetingTag(id);
            if (oracle.Length > 0) tags.Add(oracle);
            if (tags.Count == 0) continue;

            var line = $"\n<size=70%>{string.Join(" · ", tags)}</size>";
            var text = area.NameText.text ?? "";
            var cut = text.IndexOf("\n<size=70%>", System.StringComparison.Ordinal);
            if (cut >= 0) text = text[..cut];
            if (area.NameText.text != text + line) area.NameText.text = text + line;
        }
    }
}
