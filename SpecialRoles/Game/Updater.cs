using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BepInEx;
using SpecialRoles.Core;

namespace SpecialRoles.Game;

/// <summary>
/// Auto-Update über GitHub-Releases (wie CrewStats): prüft beim Spielstart im Hintergrund, ob es eine neuere Version
/// gibt, und tauscht die eigene DLL aus. Die geladene DLL darf Windows nur umbenennen, nicht löschen – deshalb:
/// alte → .old, neue an ihren Platz; .old wird beim nächsten Start gelöscht. Die neue Version läuft ab dem nächsten
/// Spielstart (Hinweis im Hauptmenü).
/// </summary>
internal static class Updater
{
    private static readonly HttpClient Http = CreateClient();

    /// <summary>Hinweis fürs Hauptmenü, sobald ein Update installiert wurde.</summary>
    public static string? Notice { get; private set; }

    private static string PluginFile
    {
        get
        {
            var location = typeof(Updater).Assembly.Location;
            return string.IsNullOrEmpty(location) ? Path.Combine(Paths.PluginPath, UpdateCheck.AssetName) : location;
        }
    }

    /// <summary>Beim Laden: Reste vom letzten Update wegräumen (die alte DLL ist jetzt nicht mehr geladen).</summary>
    public static void RemoveLeftovers()
    {
        foreach (var leftover in new[] { PluginFile + ".old", PluginFile + ".new" })
        {
            try
            {
                if (File.Exists(leftover)) File.Delete(leftover);
            }
            catch (Exception exc)
            {
                SpecialRolesPlugin.Logger.LogWarning($"Update-Rest {Path.GetFileName(leftover)} nicht löschbar: {exc.Message}");
            }
        }
    }

    public static async Task CheckAsync()
    {
        if (!SpecialRolesPlugin.AutoUpdate.Value) return;
        try
        {
            var current = UpdateCheck.ParseVersion(SpecialRolesPlugin.Version);
            if (current == null)
            {
                SpecialRolesPlugin.Logger.LogWarning($"Update-Prüfung: eigene Version nicht lesbar ({SpecialRolesPlugin.Version}).");
                return;
            }

            var update = UpdateCheck.FindNewer(await Http.GetStringAsync(UpdateCheck.ReleasesUrl), current);
            if (update == null)
            {
                SpecialRolesPlugin.Logger.LogInfo($"SpecialRoles ist aktuell ({current}).");
                return;
            }

            var bytes = await Http.GetByteArrayAsync(update.DownloadUrl);
            if (!UpdateCheck.LooksLikeDll(bytes))
            {
                SpecialRolesPlugin.Logger.LogWarning("Update-Download ist keine gültige DLL – übersprungen.");
                return;
            }

            Install(bytes);
            Notice = $"SpecialRoles {update.Version} installiert – bitte Among Us neu starten";
            SpecialRolesPlugin.Logger.LogInfo($"Update {current} → {update.Version} installiert, aktiv ab dem nächsten Start.");
        }
        catch (Exception exc)
        {
            SpecialRolesPlugin.Logger.LogWarning($"Update-Prüfung fehlgeschlagen: {exc.Message}");
        }
    }

    private static void Install(byte[] bytes)
    {
        var path = PluginFile;
        var old = path + ".old";
        var fresh = path + ".new";
        File.WriteAllBytes(fresh, bytes);
        if (File.Exists(old)) File.Delete(old);

        File.Move(path, old); // geladene DLL: umbenennen geht, löschen nicht
        try
        {
            File.Move(fresh, path);
        }
        catch
        {
            File.Move(old, path); // alte Version wiederherstellen
            throw;
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SpecialRoles-Updater"); // GitHub verlangt einen User-Agent
        return client;
    }
}
