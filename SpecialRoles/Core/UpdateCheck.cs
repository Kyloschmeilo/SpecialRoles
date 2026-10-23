using System;
using System.Text.Json;

namespace SpecialRoles.Core;

/// <summary>
/// Auswertung der GitHub-Releases fürs Auto-Update. Wie der Installer zählen auch Testversionen (Pre-Releases):
/// alle Spieler einer Lobby brauchen dieselbe Version, also sollen Installer und Updater dieselbe holen.
/// </summary>
public static class UpdateCheck
{
    /// <summary>Repo mit den Releases ("besitzer/repo"). Muss zum Installer passen ($Repo)!</summary>
    public const string GitHubRepo = "Kyloschmeilo/SpecialRoles";
    public const string AssetName = "SpecialRoles.dll";
    public const string ReleasesUrl = "https://api.github.com/repos/" + GitHubRepo + "/releases?per_page=20";

    // Nur DLLs aus den eigenen Releases laden
    private const string DownloadPrefix = "https://github.com/" + GitHubRepo + "/releases/download/";

    public sealed record Update(Version Version, string Tag, string DownloadUrl, bool Prerelease);

    /// <summary>"v1.2.0", "1.2", "1.2.0-beta" oder "1.2.0+abc123" → 1.2.0</summary>
    public static Version? ParseVersion(string text)
    {
        var core = text.Trim().TrimStart('v', 'V').Split('+', '-')[0];
        if (!Version.TryParse(core, out var version)) return null;
        return new Version(version.Major, version.Minor, Math.Max(version.Build, 0));
    }

    /// <summary>Antwort von <see cref="ReleasesUrl"/> → neueste Version mit DLL, wenn sie neuer ist als current.</summary>
    public static Update? FindNewer(string releasesJson, Version current)
    {
        using var document = JsonDocument.Parse(releasesJson);
        if (document.RootElement.ValueKind != JsonValueKind.Array) return null;
        Update? best = null;
        foreach (var release in document.RootElement.EnumerateArray())
        {
            if (IsTrue(release, "draft")) continue;
            var tag = release.TryGetProperty("tag_name", out var tagName) ? tagName.GetString() ?? "" : "";
            var version = ParseVersion(tag);
            if (version == null || version <= current || (best != null && version <= best.Version)) continue;
            var url = DllUrl(release);
            if (url != null) best = new Update(version, tag, url, IsTrue(release, "prerelease"));
        }
        return best;
    }

    private static string? DllUrl(JsonElement release)
    {
        if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) return null;
        foreach (var asset in assets.EnumerateArray())
        {
            if (!asset.TryGetProperty("name", out var name) || name.GetString() != AssetName) continue;
            var url = asset.TryGetProperty("browser_download_url", out var link) ? link.GetString() : null;
            if (url != null && url.StartsWith(DownloadPrefix, StringComparison.OrdinalIgnoreCase)) return url;
        }
        return null;
    }

    private static bool IsTrue(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;

    /// <summary>Sieht der Download wie eine Windows-DLL aus? (Schutz gegen HTML-Fehlerseiten u.ä.)</summary>
    public static bool LooksLikeDll(byte[] bytes) => bytes.Length >= 10_000 && bytes[0] == 'M' && bytes[1] == 'Z';
}
