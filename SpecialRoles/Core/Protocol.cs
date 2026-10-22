using System;
using System.Collections.Generic;
using System.IO;

namespace SpecialRoles.Core;

/// <summary>
/// Eigene Netzwerk-Nachrichten (RPC-IDs). Werte nie ändern – alle Spieler brauchen dieselbe Mod-Version.
/// „→ Host“ = nur der Host wertet aus und führt es über das Spiel aus (Kills, Wiederbeleben, Türen).
/// </summary>
public static class RpcIds
{
    /// <summary>Host → alle: Rollen und Einstellungen der Runde (<see cref="RoleSync"/>).</summary>
    public const byte SetRoles = 210;

    /// <summary>Sheriff → Host: Ziel-PlayerId.</summary>
    public const byte SheriffShoot = 211;

    /// <summary>Putzkraft → alle: PlayerId der Leiche, die verschwindet.</summary>
    public const byte CleanBody = 212;

    /// <summary>Medic → alle: PlayerId des geschützten Spielers.</summary>
    public const byte MedicShield = 213;

    /// <summary>Host → alle: ein tödlicher Angriff wurde abgewehrt (PlayerId, <see cref="Protection"/>).</summary>
    public const byte ShieldBroken = 214;

    /// <summary>Veteran → alle: ist jetzt wachsam.</summary>
    public const byte VeteranAlert = 215;

    /// <summary>Erpresser → alle: PlayerId, die im nächsten Meeting schweigen muss.</summary>
    public const byte Blackmail = 216;

    /// <summary>Transporter → alle: zwei PlayerIds + Zielpositionen; jeder versetzt nur sich selbst.</summary>
    public const byte Transport = 217;

    /// <summary>Orakel → alle: gewählte PlayerId (wird verraten, wenn das Orakel stirbt).</summary>
    public const byte OracleChoose = 218;

    /// <summary>Altruist → Host: PlayerId der Leiche.</summary>
    public const byte ReviveRequest = 219;

    /// <summary>Host → alle: PlayerId wird an Position x/y wiederbelebt.</summary>
    public const byte Revive = 220;

    /// <summary>Türwächter → Host: Räume (SystemTypes) zum Schließen.</summary>
    public const byte CloseDoors = 221;

    /// <summary>Bestatter → alle: Leiche (PlayerId) aufheben.</summary>
    public const byte DragBody = 222;

    /// <summary>Bestatter → alle: Leiche (PlayerId) an x/y ablegen.</summary>
    public const byte DropBody = 223;

    /// <summary>Vampir → Host: Ziel-PlayerId (stirbt verzögert).</summary>
    public const byte Bite = 224;

    /// <summary>Bomber → Host: Ziel-PlayerId (Bombe explodiert verzögert).</summary>
    public const byte PlantBomb = 225;

    /// <summary>Grenadier → alle: Granate an x/y.</summary>
    public const byte Grenade = 226;

    /// <summary>Maulwurf → alle: neuer Vent (Id, x, y).</summary>
    public const byte MinerVent = 227;

    /// <summary>Tarner → alle: alle sehen kurz gleich aus.</summary>
    public const byte Camouflage = 228;

    /// <summary>Hexenmeister → Host: verfluchte PlayerId soll töten.</summary>
    public const byte CurseKill = 229;

    /// <summary>Host → alle: der Narr (PlayerId) hat gewonnen, gleich danach kommt das Spielende.</summary>
    public const byte JesterWin = 230;

    /// <summary>Vergesslicher → Host: Leiche (PlayerId), deren Rolle übernommen wird.</summary>
    public const byte Remember = 231;

    /// <summary>Host → alle: PlayerId hat jetzt eine andere Mod-Rolle (+ PlayerId der übernommenen Leiche).</summary>
    public const byte RoleChange = 232;

    /// <summary>Mechaniker → Host: alle Sabotagen reparieren.</summary>
    public const byte MechanicFix = 233;

    public static bool IsOurs(byte callId) => callId is >= SetRoles and <= MechanicFix;
}

/// <summary>Was einen tödlichen Angriff abgewehrt hat (geht mit <see cref="RpcIds.ShieldBroken"/> übers Netz).</summary>
public enum Protection : byte
{
    MedicShield = 0,
    Luck = 1,
    Armor = 2,
}

/// <summary>Rollen + Einstellungen einer Runde, als Bytes für <see cref="RpcIds.SetRoles"/>.</summary>
public sealed record RoleSync(IReadOnlyList<Assignment> Assignments, RoundSettings Settings)
{
    /// <summary>2 = Einstellungen als Liste (Id, Wert); unbekannte Ids werden ignoriert.</summary>
    public const byte Version = 2;

    public byte[] Encode()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(Version);
        writer.Write((byte)Assignments.Count);
        foreach (var a in Assignments)
        {
            writer.Write(a.PlayerId);
            writer.Write((byte)a.Role);
            writer.Write((byte)a.Modifiers);
        }
        var values = new List<KeyValuePair<SettingId, float>>(Settings.Values());
        writer.Write((byte)values.Count);
        foreach (var (id, value) in values)
        {
            writer.Write((byte)id);
            writer.Write(value);
        }
        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>Unbekannte Version oder kaputte Daten → Exception (dann gibt es diese Runde keine Mod-Rollen).</summary>
    public static RoleSync Decode(byte[] data)
    {
        using var reader = new BinaryReader(new MemoryStream(data));
        var version = reader.ReadByte();
        if (version != Version)
            throw new InvalidDataException($"Rollen-Daten Version {version}, erwartet {Version} – andere Mod-Version beim Host?");
        var count = reader.ReadByte();
        var list = new List<Assignment>(count);
        for (var i = 0; i < count; i++)
        {
            var playerId = reader.ReadByte();
            var role = reader.ReadByte();
            var modifiers = reader.ReadByte();
            list.Add(new Assignment(playerId, RoleCatalog.IsKnown(role) ? (RoleId)role : RoleId.None, (Modifier)modifiers & Modifier.Flash));
        }
        var settingCount = reader.ReadByte();
        var values = new List<KeyValuePair<SettingId, float>>(settingCount);
        for (var i = 0; i < settingCount; i++)
        {
            var id = (SettingId)reader.ReadByte();
            values.Add(new KeyValuePair<SettingId, float>(id, reader.ReadSingle()));
        }
        return new RoleSync(list, new RoundSettings(values));
    }
}
