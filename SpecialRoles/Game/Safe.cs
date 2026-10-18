using System;

namespace SpecialRoles.Game;

internal static class Safe
{
    /// <summary>Fehler der Mod dürfen das Spiel nie stören – nur ins Log.</summary>
    public static void Run(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exc)
        {
            SpecialRolesPlugin.Logger.LogError($"SpecialRoles-Fehler (Spiel läuft weiter): {exc}");
        }
    }
}
