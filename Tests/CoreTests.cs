using System;
using System.Collections.Generic;
using System.Linq;
using SpecialRoles.Core;
using Xunit;

namespace SpecialRoles.Tests;

public class RoleAssignerTests
{
    private static readonly Random Seeded = new(1234);
    private static int Next(int n) => Seeded.Next(n);

    private static List<PlayerSlot> Lobby(int crew, int impostors, int withGameRole = 0) =>
        Enumerable.Range(0, crew).Select(i => new PlayerSlot((byte)i, false, i >= withGameRole))
            .Concat(Enumerable.Range(crew, impostors).Select(i => new PlayerSlot((byte)i, true, true)))
            .ToList();

    [Fact]
    public void SureRolesGoToTheRightTeam()
    {
        var players = Lobby(8, 2);
        var roles = new[]
        {
            new RoleSetting(RoleId.Sheriff, 1, 100),
            new RoleSetting(RoleId.NightVision, 1, 100),
            new RoleSetting(RoleId.Janitor, 1, 100),
        };
        var result = RoleAssigner.Assign(players, roles, 0, 0, Next);

        Assert.Equal(3, result.Count);
        foreach (var a in result)
        {
            var isImpostor = players.Single(p => p.PlayerId == a.PlayerId).IsImpostor;
            Assert.Equal(RoleCatalog.Get(a.Role)!.Team == RoleTeam.Impostor, isImpostor);
        }
    }

    [Fact]
    public void PlayersWithAGameRoleGetNoModRole()
    {
        var players = Lobby(3, 1, withGameRole: 2); // Spieler 0 und 1 sind z.B. Engineer
        var result = RoleAssigner.Assign(players, new[] { new RoleSetting(RoleId.Sheriff, 3, 100) }, 0, 0, Next);
        Assert.Equal(new byte[] { 2 }, result.Select(a => a.PlayerId).ToArray());
    }

    [Fact]
    public void MoreRolesThanSlotsDropsTheRest()
    {
        var result = RoleAssigner.Assign(Lobby(2, 1), new[] { new RoleSetting(RoleId.Sheriff, 5, 100) }, 0, 0, Next);
        Assert.Equal(2, result.Count(a => a.Role == RoleId.Sheriff));
    }

    [Fact]
    public void ZeroChanceOrCountMeansOff()
    {
        var roles = new[] { new RoleSetting(RoleId.Sheriff, 3, 0), new RoleSetting(RoleId.Janitor, 0, 100) };
        Assert.Empty(RoleAssigner.Assign(Lobby(8, 2), roles, 1, 0, Next));
    }

    [Fact]
    public void FlashCanStackWithARole()
    {
        var players = Lobby(1, 0);
        var result = RoleAssigner.Assign(players, new[] { new RoleSetting(RoleId.Sheriff, 1, 100) }, 1, 100, Next);
        Assert.Equal(new Assignment(0, RoleId.Sheriff, Modifier.Flash), Assert.Single(result));
    }

    [Fact]
    public void EveryCrewmateHasTheSameChance()
    {
        var rng = new Random(42);
        var hits = new int[8];
        const int Runs = 40000;
        for (var run = 0; run < Runs; run++)
        {
            foreach (var a in RoleAssigner.Assign(Lobby(8, 2), new[] { new RoleSetting(RoleId.Sheriff, 1, 100) }, 0, 0, rng.Next))
                hits[a.PlayerId]++;
        }
        foreach (var h in hits)
            Assert.InRange(h, Runs / 8 * 0.94, Runs / 8 * 1.06);
    }

    [Fact]
    public void ChanceIsRespected()
    {
        var rng = new Random(7);
        var count = 0;
        const int Runs = 20000;
        for (var run = 0; run < Runs; run++)
            count += RoleAssigner.Assign(Lobby(8, 2), new[] { new RoleSetting(RoleId.Sheriff, 1, 30) }, 0, 0, rng.Next).Count;
        Assert.InRange(count, Runs * 0.28, Runs * 0.32);
    }
}

public class ProtocolTests
{
    [Fact]
    public void RoundTrip()
    {
        var sync = new RoleSync(
            new[] { new Assignment(3, RoleId.Sheriff, Modifier.None), new Assignment(7, RoleId.Janitor, Modifier.Flash) },
            Settings((SettingId.SheriffCooldown, 30f), (SettingId.FlashSpeed, 1.5f), (SettingId.LuckyChance, 70f)));
        var decoded = RoleSync.Decode(sync.Encode());
        Assert.Equal(sync.Assignments, decoded.Assignments);
        Assert.Equal(sync.Settings, decoded.Settings);
        Assert.Equal(30f, decoded.Settings.SheriffCooldown);
        Assert.Equal(70, decoded.Settings.GetInt(SettingId.LuckyChance));
        Assert.Equal(10f, decoded.Settings.Get(SettingId.VampireDelay)); // Standardwert
    }

    private static RoundSettings Settings(params (SettingId Id, float Value)[] values) =>
        new(values.Select(v => new KeyValuePair<SettingId, float>(v.Id, v.Value)));

    [Fact]
    public void UnknownRolesAndFlagsAreIgnored()
    {
        var data = new RoleSync(new[] { new Assignment(1, (RoleId)199, (Modifier)0xFF) }, RoundSettings.Default).Encode();
        Assert.Equal(new Assignment(1, RoleId.None, Modifier.Flash), Assert.Single(RoleSync.Decode(data).Assignments));
    }

    [Fact]
    public void OtherVersionIsRejected()
    {
        var data = new RoleSync(Array.Empty<Assignment>(), RoundSettings.Default).Encode();
        data[0] = 99;
        Assert.ThrowsAny<Exception>(() => RoleSync.Decode(data));
    }

    [Fact]
    public void SettingsAreClamped()
    {
        var data = new RoleSync(Array.Empty<Assignment>(), Settings((SettingId.SheriffCooldown, 9999f), (SettingId.FlashSpeed, float.NaN))).Encode();
        var settings = RoleSync.Decode(data).Settings;
        Assert.Equal(120f, settings.SheriffCooldown);
        Assert.Equal(1f, settings.FlashSpeed);
    }

    [Fact]
    public void UnknownSettingsAreIgnored()
    {
        // Version 2, keine Rollen, zwei Einstellungen: unbekannte Id 250 und Sheriff-Cooldown 40
        var data = new byte[] { 2, 0, 2, 250 }.Concat(BitConverter.GetBytes(5f))
            .Concat(new byte[] { (byte)SettingId.SheriffCooldown }).Concat(BitConverter.GetBytes(40f)).ToArray();
        var settings = RoleSync.Decode(data).Settings;
        Assert.Equal(40f, settings.SheriffCooldown);
        Assert.Equal(RoundSettings.Default.FlashSpeed, settings.FlashSpeed);
    }
}

public class CatalogTests
{
    [Fact]
    public void ThirtyRolesWithUniqueIdsAndNames()
    {
        Assert.Equal(30, RoleCatalog.All.Count);
        Assert.Equal(17, RoleCatalog.All.Count(r => r.Team == RoleTeam.Crew));
        Assert.Equal(11, RoleCatalog.All.Count(r => r.Team == RoleTeam.Impostor));
        Assert.Equal(2, RoleCatalog.All.Count(r => r.Team == RoleTeam.Neutral));
        Assert.Equal(30, RoleCatalog.All.Select(r => r.Id).Distinct().Count());
        Assert.Equal(30, RoleCatalog.All.Select(r => r.Name).Distinct().Count());
        Assert.All(RoleCatalog.All, r => Assert.Matches("^[0-9A-F]{6}$", r.Color));
    }

    [Fact]
    public void SettingsAreConsistent()
    {
        Assert.Equal(SettingCatalog.All.Count, SettingCatalog.All.Select(s => s.Id).Distinct().Count());
        Assert.Equal(SettingCatalog.All.Count, SettingCatalog.All.Select(s => (s.Section, s.Key)).Distinct().Count());
        Assert.All(SettingCatalog.All, s => Assert.InRange(s.Default, s.Min, s.Max));
        // Abschnitt = Rollenname, damit die Config nach Rollen sortiert ist
        Assert.All(SettingCatalog.All.Where(s => s.Role != RoleId.None), s => Assert.Equal(RoleCatalog.Get(s.Role)!.Name, s.Section));
    }
}

public class PngTests
{
    [Fact]
    public void EncodesRgbaImageThatInflatesBack()
    {
        var rgba = new byte[] { 255, 0, 0, 255, 0, 255, 0, 128, 0, 0, 255, 0, 255, 255, 255, 255, 1, 2, 3, 4, 5, 6, 7, 8 };
        var png = Png.Encode(3, 2, rgba);

        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, png[..8]);
        var chunks = Chunks(png);
        Assert.Equal(new[] { "IHDR", "IDAT", "IEND" }, chunks.Select(c => c.Type));

        var header = chunks[0].Data;
        Assert.Equal(3, ReadInt(header, 0));
        Assert.Equal(2, ReadInt(header, 4));
        Assert.Equal(8, header[8]);
        Assert.Equal(6, header[9]);

        using var inflate = new System.IO.Compression.ZLibStream(new System.IO.MemoryStream(chunks[1].Data),
            System.IO.Compression.CompressionMode.Decompress);
        using var raw = new System.IO.MemoryStream();
        inflate.CopyTo(raw);
        var expected = new List<byte>();
        for (var y = 0; y < 2; y++)
        {
            expected.Add(0);
            expected.AddRange(rgba.Skip(y * 12).Take(12));
        }
        Assert.Equal(expected.ToArray(), raw.ToArray());
    }

    private static int ReadInt(byte[] data, int offset) =>
        (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];

    private static List<(string Type, byte[] Data)> Chunks(byte[] png)
    {
        var result = new List<(string, byte[])>();
        var pos = 8;
        while (pos < png.Length)
        {
            var length = ReadInt(png, pos);
            var type = System.Text.Encoding.ASCII.GetString(png, pos + 4, 4);
            var data = png[(pos + 8)..(pos + 8 + length)];
            var crc = (uint)ReadInt(png, pos + 8 + length);
            Assert.Equal(Crc32(png[(pos + 4)..(pos + 8 + length)]), crc);
            result.Add((type, data));
            pos += 12 + length;
        }
        return result;
    }

    private static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }
        return crc ^ 0xFFFFFFFFu;
    }
}

public class ButtonTextTests
{
    [Fact]
    public void SharpSBecomesDoubleS()
    {
        Assert.Equal("Schiessen", ButtonText.Safe("Schießen"));
        Assert.Equal("Wählen", ButtonText.Safe("Wählen")); // Umlaute hat die Schrift
    }
}
