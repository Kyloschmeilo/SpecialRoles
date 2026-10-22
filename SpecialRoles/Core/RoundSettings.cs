using System;
using System.Collections.Generic;
using System.Linq;

namespace SpecialRoles.Core;

/// <summary>Einstellungen, die der Host mit den Rollen an alle schickt. Werte gehen übers Netz: nie ändern, nur anhängen.</summary>
public enum SettingId : byte
{
    SheriffCooldown = 1,
    FlashSpeed = 2,
    HackerCooldown = 3,
    HackerDuration = 4,
    TrackerCooldown = 5,
    TrackerDuration = 6,
    TrapperTraps = 7,
    MediumCooldown = 8,
    VeteranCooldown = 9,
    VeteranDuration = 10,
    VeteranUses = 11,
    TransporterCooldown = 12,
    TransporterUses = 13,
    CameramanCooldown = 14,
    CameramanDuration = 15,
    DoorkeeperCooldown = 16,
    LuckyChance = 17,
    UndertakerSpeed = 18,
    VampireDelay = 19,
    BomberDelay = 20,
    BomberRadius = 21,
    GrenadierCooldown = 22,
    GrenadierDuration = 23,
    GrenadierRadius = 24,
    MinerVents = 25,
    BlackmailerCooldown = 26,
    CamouflagerCooldown = 27,
    CamouflagerDuration = 28,
    SaboteurFactor = 29,
    MedicCooldown = 30,
    OracleCooldown = 31,
}

/// <param name="Section">Abschnitt in der Config (Rollenname, Flash unter „Modifikatoren“).</param>
/// <param name="Role">Rolle, zu der die Einstellung gehört – nur fertige Rollen bekommen Config-Einträge.</param>
public sealed record SettingDef(SettingId Id, RoleId Role, string Section, string Key, float Default, float Min, float Max, string Description);

public static class SettingCatalog
{
    public static readonly IReadOnlyList<SettingDef> All = new[]
    {
        new SettingDef(SettingId.SheriffCooldown, RoleId.Sheriff, "Sheriff", "Cooldown", 25f, 5f, 120f, "Sekunden zwischen zwei Schüssen."),
        new SettingDef(SettingId.FlashSpeed, RoleId.None, "Modifikatoren", "Flash Tempo", 1.25f, 1f, 2f, "Lauftempo von Flash (1.25 = 25 % schneller)."),
        new SettingDef(SettingId.MedicCooldown, RoleId.Medic, "Medic", "Cooldown", 10f, 0f, 120f, "Sekunden, bis nach einem gebrochenen Schild ein neuer geht."),
        new SettingDef(SettingId.HackerCooldown, RoleId.Hacker, "Hacker", "Cooldown", 25f, 5f, 120f, "Sekunden zwischen zwei Blicken auf die Admin-Karte."),
        new SettingDef(SettingId.HackerDuration, RoleId.Hacker, "Hacker", "Dauer", 8f, 2f, 30f, "Wie lange die Admin-Karte offen bleibt (Sekunden)."),
        new SettingDef(SettingId.TrackerCooldown, RoleId.Tracker, "Spurenleser", "Cooldown", 25f, 5f, 120f, "Sekunden zwischen zwei Spurensuchen."),
        new SettingDef(SettingId.TrackerDuration, RoleId.Tracker, "Spurenleser", "Dauer", 8f, 2f, 30f, "Wie lange die Fußspuren sichtbar sind (Sekunden)."),
        new SettingDef(SettingId.TrapperTraps, RoleId.Trapper, "Fallensteller", "Fallen", 3f, 1f, 10f, "Wie viele Fallen pro Spiel."),
        new SettingDef(SettingId.MediumCooldown, RoleId.Medium, "Medium", "Cooldown", 20f, 5f, 120f, "Sekunden zwischen zwei Blicken an einer Leiche."),
        new SettingDef(SettingId.OracleCooldown, RoleId.Oracle, "Orakel", "Cooldown", 15f, 0f, 120f, "Sekunden, bis du einen anderen Spieler wählen kannst."),
        new SettingDef(SettingId.VeteranCooldown, RoleId.Veteran, "Veteran", "Cooldown", 25f, 5f, 120f, "Sekunden zwischen zwei Wachsamkeiten."),
        new SettingDef(SettingId.VeteranDuration, RoleId.Veteran, "Veteran", "Dauer", 10f, 2f, 30f, "Wie lange du wachsam bist (Sekunden)."),
        new SettingDef(SettingId.VeteranUses, RoleId.Veteran, "Veteran", "Einsätze", 3f, 1f, 10f, "Wie oft pro Spiel."),
        new SettingDef(SettingId.TransporterCooldown, RoleId.Transporter, "Transporter", "Cooldown", 25f, 5f, 120f, "Sekunden zwischen zwei Tauschen."),
        new SettingDef(SettingId.TransporterUses, RoleId.Transporter, "Transporter", "Einsätze", 5f, 1f, 15f, "Wie oft pro Spiel."),
        new SettingDef(SettingId.CameramanCooldown, RoleId.Cameraman, "Kameramann", "Cooldown", 15f, 0f, 120f, "Sekunden zwischen zwei Blicken durch eine Kamera."),
        new SettingDef(SettingId.CameramanDuration, RoleId.Cameraman, "Kameramann", "Dauer", 8f, 2f, 30f, "Wie lange du durch die Kamera schaust (Sekunden)."),
        new SettingDef(SettingId.DoorkeeperCooldown, RoleId.Doorkeeper, "Türwächter", "Cooldown", 30f, 10f, 120f, "Sekunden zwischen zwei Mal Türen schließen."),
        new SettingDef(SettingId.LuckyChance, RoleId.Lucky, "Glückspilz", "Chance", 50f, 0f, 100f, "Chance in Prozent, einen Angriff zu überleben (einmal pro Spiel)."),
        new SettingDef(SettingId.UndertakerSpeed, RoleId.Undertaker, "Bestatter", "Tempo beim Tragen", 0.75f, 0.3f, 1f, "Lauftempo mit Leiche (0.75 = 25 % langsamer)."),
        new SettingDef(SettingId.VampireDelay, RoleId.Vampire, "Vampir", "Verzögerung", 10f, 3f, 30f, "Sekunden, bis das Opfer nach dem Biss stirbt."),
        new SettingDef(SettingId.BomberDelay, RoleId.Bomber, "Bomber", "Verzögerung", 10f, 3f, 30f, "Sekunden bis zur Explosion."),
        new SettingDef(SettingId.BomberRadius, RoleId.Bomber, "Bomber", "Radius", 1.5f, 0.5f, 4f, "Wer so nah am Träger steht, stirbt mit (Impostor nicht)."),
        new SettingDef(SettingId.GrenadierCooldown, RoleId.Grenadier, "Grenadier", "Cooldown", 30f, 10f, 120f, "Sekunden zwischen zwei Granaten."),
        new SettingDef(SettingId.GrenadierDuration, RoleId.Grenadier, "Grenadier", "Dauer", 6f, 2f, 15f, "Wie lange die Crew geblendet ist (Sekunden)."),
        new SettingDef(SettingId.GrenadierRadius, RoleId.Grenadier, "Grenadier", "Radius", 5f, 1f, 15f, "Wer so nah dran ist, wird geblendet."),
        new SettingDef(SettingId.MinerVents, RoleId.Miner, "Maulwurf", "Vents", 3f, 2f, 6f, "Wie viele eigene Vents pro Spiel (sie sind miteinander verbunden)."),
        new SettingDef(SettingId.BlackmailerCooldown, RoleId.Blackmailer, "Erpresser", "Cooldown", 25f, 5f, 120f, "Sekunden zwischen zwei Erpressungen."),
        new SettingDef(SettingId.CamouflagerCooldown, RoleId.Camouflager, "Tarner", "Cooldown", 30f, 10f, 120f, "Sekunden zwischen zwei Tarnungen."),
        new SettingDef(SettingId.CamouflagerDuration, RoleId.Camouflager, "Tarner", "Dauer", 10f, 3f, 30f, "Wie lange alle gleich aussehen (Sekunden)."),
        new SettingDef(SettingId.SaboteurFactor, RoleId.Saboteur, "Saboteur", "Cooldown-Faktor", 0.5f, 0.1f, 1f, "Sabotage-Cooldown mal diesem Faktor, solange ein Saboteur lebt."),
    };

    private static readonly Dictionary<SettingId, SettingDef> ById = All.ToDictionary(s => s.Id);

    public static SettingDef? Get(SettingId id) => ById.TryGetValue(id, out var def) ? def : null;

    /// <summary>Unbekannt → null; NaN → Minimum; sonst auf [Min, Max] begrenzt.</summary>
    public static float? Sanitize(SettingId id, float value)
    {
        var def = Get(id);
        if (def == null) return null;
        return float.IsNaN(value) ? def.Min : Math.Clamp(value, def.Min, def.Max);
    }
}

/// <summary>Einstellungen einer Runde (fehlende = Standardwert). Kommt vom Host mit <see cref="RoleSync"/>.</summary>
public sealed class RoundSettings : IEquatable<RoundSettings>
{
    private readonly SortedDictionary<SettingId, float> _values = new();

    public static readonly RoundSettings Default = new();

    public RoundSettings(IEnumerable<KeyValuePair<SettingId, float>>? values = null)
    {
        if (values == null) return;
        foreach (var (id, value) in values)
        {
            var sane = SettingCatalog.Sanitize(id, value);
            if (sane != null) _values[id] = sane.Value;
        }
    }

    public float Get(SettingId id) =>
        _values.TryGetValue(id, out var value) ? value : SettingCatalog.Get(id)?.Default ?? 0f;

    public int GetInt(SettingId id) => (int)Math.Round(Get(id));

    /// <summary>Alle Werte inkl. Standardwerten (zum Senden).</summary>
    public IEnumerable<KeyValuePair<SettingId, float>> Values() =>
        SettingCatalog.All.Select(def => new KeyValuePair<SettingId, float>(def.Id, Get(def.Id)));

    public float SheriffCooldown => Get(SettingId.SheriffCooldown);
    public float FlashSpeed => Get(SettingId.FlashSpeed);

    public bool Equals(RoundSettings? other) =>
        other != null && SettingCatalog.All.All(def => Get(def.Id).Equals(other.Get(def.Id)));

    public override bool Equals(object? obj) => Equals(obj as RoundSettings);

    public override int GetHashCode() => SettingCatalog.All.Aggregate(17, (hash, def) => hash * 31 + Get(def.Id).GetHashCode());
}
