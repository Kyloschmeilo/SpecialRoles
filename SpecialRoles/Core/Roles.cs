using System.Collections.Generic;
using System.Linq;

namespace SpecialRoles.Core;

// Spielunabhängig (kein Unity/BepInEx) – wird auch von den Tests kompiliert.

/// <summary>Rollen-IDs. Werden übers Netz geschickt: Werte nie ändern, neue nur anhängen.</summary>
public enum RoleId : byte
{
    None = 0,

    // Crewmates
    Sheriff = 1,
    Medic = 2,
    Mayor = 3,
    Snitch = 4,
    Hacker = 5,
    Tracker = 6,
    Trapper = 7,
    Medium = 8,
    Veteran = 9,
    NightVision = 10,
    Altruist = 11,
    Mechanic = 12,
    Transporter = 13,
    Oracle = 14,
    Cameraman = 15,
    Doorkeeper = 16,
    Lucky = 17,

    // Impostor
    Janitor = 40,
    Undertaker = 41,
    Vampire = 42,
    Bomber = 43,
    Grenadier = 44,
    Miner = 45,
    Blackmailer = 46,
    Camouflager = 47,
    Warlock = 48,
    Armor = 49,
    Saboteur = 50,

    // Neutral
    Jester = 70,
    Amnesiac = 71,
}

/// <summary>Modifikatoren kommen zusätzlich zur Rolle (Bitmaske, wird übers Netz geschickt).</summary>
[System.Flags]
public enum Modifier : byte
{
    None = 0,
    Flash = 1,
}

/// <summary>Crew- und Neutral-Rollen bekommen Spieler, die im Spiel Crewmate sind; Impostor-Rollen nur Impostor.</summary>
public enum RoleTeam
{
    Crew,
    Impostor,
    Neutral,
}

/// <param name="Name">Anzeigename (deutsch, wie in /special-rolle im Discord-Bot).</param>
/// <param name="Color">Farbe als Hex ohne #, für Name und Aufgabenliste.</param>
/// <param name="Blurb">Kurzbeschreibung für die Aufgabenliste.</param>
/// <param name="Implemented">Nur fertige Rollen werden vergeben.</param>
public sealed record RoleInfo(RoleId Id, string Name, RoleTeam Team, string Color, string Blurb, bool Implemented = false);

public static class RoleCatalog
{
    public const string FlashColor = "FFD54F";

    public static readonly IReadOnlyList<RoleInfo> All = new[]
    {
        // ---------- Crewmates ----------
        new RoleInfo(RoleId.Sheriff, "Sheriff", RoleTeam.Crew, "F8CD46", "Erschieße die Impostor – triffst du einen Crewmate, stirbst du selbst.", true),
        new RoleInfo(RoleId.Medic, "Medic", RoleTeam.Crew, "7EFBC2", "Gib einem Spieler einen Schild, der einen Angriff abwehrt.", true),
        new RoleInfo(RoleId.Mayor, "Bürgermeister", RoleTeam.Crew, "704FA8", "Deine Stimme zählt im Meeting doppelt.", true),
        new RoleInfo(RoleId.Snitch, "Snitch", RoleTeam.Crew, "D4AF37", "Erledige alle Tasks, dann zeigen Pfeile auf die Impostor.", true),
        new RoleInfo(RoleId.Hacker, "Hacker", RoleTeam.Crew, "3CB371", "Sieh dir kurz die Admin-Karte von überall an.", true),
        new RoleInfo(RoleId.Tracker, "Spurenleser", RoleTeam.Crew, "A67C52", "Sieh kurz die Fußspuren aller Spieler.", true),
        new RoleInfo(RoleId.Trapper, "Fallensteller", RoleTeam.Crew, "8FBC8F", "Stell Fallen auf und erfahre, wer hineinläuft.", true),
        new RoleInfo(RoleId.Medium, "Medium", RoleTeam.Crew, "A680FF", "Sieh an einer frischen Leiche, woher der Mörder kam.", true),
        new RoleInfo(RoleId.Veteran, "Veteran", RoleTeam.Crew, "998040", "Werde kurz wachsam – wer dich dann angreift, stirbt.", true),
        new RoleInfo(RoleId.NightVision, "Nachtsicht", RoleTeam.Crew, "4FC3F7", "Bei Licht-Sabotage siehst du ganz normal weiter.", true),
        new RoleInfo(RoleId.Altruist, "Altruist", RoleTeam.Crew, "FF6F91", "Belebe eine Leiche wieder – du stirbst dabei selbst.", true),
        new RoleInfo(RoleId.Mechanic, "Mechaniker", RoleTeam.Crew, "B0BEC5", "Repariere einmal pro Runde eine Sabotage von überall.", true),
        new RoleInfo(RoleId.Transporter, "Transporter", RoleTeam.Crew, "00ACC1", "Tausche die Plätze zweier Spieler.", true),
        new RoleInfo(RoleId.Oracle, "Orakel", RoleTeam.Crew, "BF00BF", "Stirbst du, wird das Team deines gewählten Spielers verraten.", true),
        new RoleInfo(RoleId.Cameraman, "Kameramann", RoleTeam.Crew, "607D8B", "Stell zwei eigene Kameras auf und schau hindurch (Taste G).", true),
        new RoleInfo(RoleId.Doorkeeper, "Türwächter", RoleTeam.Crew, "8D6E63", "Schließ kurz alle Türen in deiner Nähe.", true),
        new RoleInfo(RoleId.Lucky, "Glückspilz", RoleTeam.Crew, "66BB6A", "Mit etwas Glück überlebst du einen Angriff.", true),

        // ---------- Impostor ----------
        new RoleInfo(RoleId.Janitor, "Putzkraft", RoleTeam.Impostor, "C62828", "Lass Leichen verschwinden (teilt sich den Cooldown mit dem Kill).", true),
        new RoleInfo(RoleId.Undertaker, "Bestatter", RoleTeam.Impostor, "8E2424", "Trag Leichen weg und leg sie woanders ab.", true),
        new RoleInfo(RoleId.Vampire, "Vampir", RoleTeam.Impostor, "A31515", "Beiß zu – dein Opfer stirbt erst ein paar Sekunden später.", true),
        new RoleInfo(RoleId.Bomber, "Bomber", RoleTeam.Impostor, "D84315", "Kleb jemandem eine Bombe an – sie explodiert kurz danach.", true),
        new RoleInfo(RoleId.Grenadier, "Grenadier", RoleTeam.Impostor, "E65100", "Blende die Crew mit einer Granate.", true),
        new RoleInfo(RoleId.Miner, "Maulwurf", RoleTeam.Impostor, "795548", "Bau eigene Vents.", true),
        new RoleInfo(RoleId.Blackmailer, "Erpresser", RoleTeam.Impostor, "4A148C", "Bring einen Spieler im Meeting zum Schweigen (2× pro Spiel).", true),
        new RoleInfo(RoleId.Camouflager, "Tarner", RoleTeam.Impostor, "757575", "Lass kurz alle gleich grau aussehen.", true),
        new RoleInfo(RoleId.Warlock, "Hexenmeister", RoleTeam.Impostor, "6A1B9A", "Verfluche einen Spieler – er tötet für dich.", true),
        new RoleInfo(RoleId.Armor, "Armor", RoleTeam.Impostor, "9E9E9E", "Du überlebst den ersten tödlichen Treffer.", true),
        new RoleInfo(RoleId.Saboteur, "Saboteur", RoleTeam.Impostor, "B71C1C", "Deine Sabotagen sind schneller wieder bereit.", true),

        // ---------- Neutral ----------
        new RoleInfo(RoleId.Jester, "Narr", RoleTeam.Neutral, "EC62A5", "Lass dich rauswählen, dann gewinnst du.", true),
        new RoleInfo(RoleId.Amnesiac, "Vergesslicher", RoleTeam.Neutral, "80B2FF", "Übernimm die Rolle einer Leiche.", true),
    };

    private static readonly Dictionary<RoleId, RoleInfo> ById = All.ToDictionary(r => r.Id);

    public static RoleInfo? Get(RoleId id) => ById.TryGetValue(id, out var info) ? info : null;

    public static bool IsKnown(byte id) => ById.ContainsKey((RoleId)id);
}
