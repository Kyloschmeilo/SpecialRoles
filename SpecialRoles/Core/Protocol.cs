using System;
using System.Collections.Generic;
using System.IO;

namespace SpecialRoles.Core;

/// <summary>Eigene Netzwerk-Nachrichten (RPC-IDs). Werte nie ändern – alle Spieler brauchen dieselbe Mod-Version.</summary>
public static class RpcIds
{
    /// <summary>Host → alle: Rollen und Einstellungen der Runde (<see cref="RoleSync"/>).</summary>
    public const byte SetRoles = 210;

    /// <summary>Sheriff → alle (nur der Host wertet aus): Ziel-PlayerId.</summary>
    public const byte SheriffShoot = 211;

    /// <summary>Putzkraft → alle: PlayerId der Leiche, die verschwindet.</summary>
    public const byte CleanBody = 212;
}

/// <summary>Einstellungen, die alle Spieler kennen müssen (der Host schickt seine mit den Rollen).</summary>
public sealed record RoundSettings(float SheriffCooldown, float FlashSpeed)
{
    public static readonly RoundSettings Default = new(25f, 1.25f);
}

/// <summary>Rollen + Einstellungen einer Runde, als Bytes für <see cref="RpcIds.SetRoles"/>.</summary>
public sealed record RoleSync(IReadOnlyList<Assignment> Assignments, RoundSettings Settings)
{
    public const byte Version = 1;

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
        writer.Write(Settings.SheriffCooldown);
        writer.Write(Settings.FlashSpeed);
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
        var settings = new RoundSettings(Sane(reader.ReadSingle(), 1f, 120f), Sane(reader.ReadSingle(), 1f, 2f));
        return new RoleSync(list, settings);
    }

    private static float Sane(float value, float min, float max) =>
        float.IsNaN(value) ? min : Math.Clamp(value, min, max);
}
