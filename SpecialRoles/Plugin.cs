using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace SpecialRoles;

/// <summary>
/// 30 neue Rollen für Among Us. Der Host verteilt sie beim Rundenstart zusätzlich zu den Spielrollen und schickt sie
/// an alle; jeder sieht nur seine eigene Rolle. Alle Spieler der Lobby brauchen dieselbe Mod-Version.
/// Unabhängig von CrewStats (beide können gleichzeitig installiert sein).
/// </summary>
[BepInAutoPlugin("de.spielenetzwerk.specialroles")]
[BepInProcess("Among Us.exe")]
public partial class SpecialRolesPlugin : BasePlugin
{
    internal static ManualLogSource Logger { get; private set; } = null!;

    public Harmony Harmony { get; } = new(Id);

    public override void Load()
    {
        Logger = Log;
        Settings.Bind(Config);
        Harmony.PatchAll();
        Log.LogInfo($"SpecialRoles {Version} geladen.");
    }
}
