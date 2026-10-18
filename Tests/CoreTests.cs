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
            new RoundSettings(30f, 1.5f));
        var decoded = RoleSync.Decode(sync.Encode());
        Assert.Equal(sync.Assignments, decoded.Assignments);
        Assert.Equal(sync.Settings, decoded.Settings);
    }

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
        var data = new RoleSync(Array.Empty<Assignment>(), new RoundSettings(9999f, float.NaN)).Encode();
        Assert.Equal(new RoundSettings(120f, 1f), RoleSync.Decode(data).Settings);
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
}
