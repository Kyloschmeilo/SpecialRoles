using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using SpecialRoles.Game;

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
    internal static ConfigEntry<bool> AutoUpdate { get; private set; } = null!;

    public Harmony Harmony { get; } = new(Id);

    public override void Load()
    {
        Logger = Log;
        Settings.Bind(Config);
        AutoUpdate = Config.Bind("Update", "AutoUpdate", true,
            "Beim Spielstart auf GitHub nach einer neuen SpecialRoles-Version suchen und sie installieren " +
            "(aktiv ab dem nächsten Start).");
        Harmony.PatchAll();
        Log.LogInfo($"SpecialRoles {Version} geladen.");
        Updater.RemoveLeftovers();
        Task.Run(Updater.CheckAsync);
    }
}
