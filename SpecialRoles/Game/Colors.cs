using SpecialRoles.Core;
using UnityEngine;

namespace SpecialRoles.Game;

internal static class Colors
{
    /// <summary>„RRGGBB“ → Farbe (ungültig = weiß).</summary>
    public static Color FromHex(string hex, float alpha = 1f)
    {
        if (hex.Length != 6 || !uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
            return new Color(1f, 1f, 1f, alpha);
        return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
    }

    public static Color Of(RoleId role, float alpha = 1f) => FromHex(RoleCatalog.Get(role)?.Color ?? "FFFFFF", alpha);

    /// <summary>Text in der Farbe einer Rolle (Rich Text).</summary>
    public static string Paint(RoleId role, string text) => $"<color=#{RoleCatalog.Get(role)?.Color ?? "FFFFFF"}>{text}</color>";
}
