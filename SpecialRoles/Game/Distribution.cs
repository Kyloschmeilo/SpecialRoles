using System.Collections.Generic;
using System.Security.Cryptography;
using AmongUs.GameOptions;
using SpecialRoles.Core;

namespace SpecialRoles.Game;

/// <summary>Host: verteilt nach der Rollenvergabe des Spiels die Mod-Rollen und schickt sie an alle.</summary>
internal static class Distribution
{
    public static void HostAssign()
    {
        var client = AmongUsClient.Instance;
        if (client == null || !client.AmHost || GameData.Instance == null) return;
        var options = GameOptionsManager.Instance?.CurrentGameOptions;
        if (options == null || options.GameMode is not (GameModes.Normal or GameModes.NormalFools))
        {
            Round.NewGame();
            return; // Hide & Seek: keine Mod-Rollen
        }

        var players = new List<PlayerSlot>();
        foreach (var info in GameData.Instance.AllPlayers)
        {
            if (info == null || info.Disconnected) continue;
            var role = info.RoleType;
            players.Add(new PlayerSlot(info.PlayerId, RoleManager.IsImpostorRole(role), role is RoleTypes.Crewmate or RoleTypes.Impostor));
        }

        var assignments = RoleAssigner.Assign(players, Settings.RoleSettings(), Settings.FlashCount, Settings.FlashChance,
            RandomNumberGenerator.GetInt32);
        var sync = new RoleSync(assignments, Settings.Round());
        Net.Send(RpcIds.SetRoles, writer => writer.WriteBytesAndSize(sync.Encode()));
        RoleState.Apply(sync);
        SpecialRolesPlugin.Logger.LogInfo($"Mod-Rollen verteilt: {assignments.Count} von {players.Count} Spielern.");
    }
}
