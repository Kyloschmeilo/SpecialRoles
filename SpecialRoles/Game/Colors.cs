using SpecialRoles.Core;
using UnityEngine;

namespace SpecialRoles.Game;

internal static class Colors
{
    /// <summary>„RRGGBB“ → Farbe (ungültig = weiß).</summary>
    public static Color FromHex(string hex, float alpha = 1f)
    {
        if (!ColorUtility.TryParseHtmlString("#" + hex, out var color)) color = Color.white;
        color.a = alpha;
        return color;
    }

    public static Color Of(RoleId role, float alpha = 1f) => FromHex(RoleCatalog.Get(role)?.Color ?? "FFFFFF", alpha);

    /// <summary>Text in der Farbe einer Rolle (Rich Text).</summary>
    public static string Paint(RoleId role, string text) => $"<color=#{RoleCatalog.Get(role)?.Color ?? "FFFFFF"}>{text}</color>";
}
