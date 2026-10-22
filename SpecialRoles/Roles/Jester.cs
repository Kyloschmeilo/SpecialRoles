using AmongUs.GameOptions;
using SpecialRoles.Core;
using SpecialRoles.Game;

namespace SpecialRoles.Roles;

/// <summary>
/// Narr (Neutral, im Spiel Crewmate): wird er rausgewählt, gewinnt er allein. Der Host beendet dann das Spiel mit einem
/// eigenen Grund (CrewStats wertet ihn als „unbekannt“, gibt also keiner Seite Siege); alle Clients zeigen den Narren
/// als einzigen Gewinner.
/// </summary>
internal static class Jester
{
    /// <summary>Eigener Spielende-Grund (das Spiel kennt 0–8).</summary>
    public const GameOverReason WinReason = (GameOverReason)70;

    private static byte? _winner;
    private static CachedPlayerData? _winnerData;

    public static bool WinPending => _winner != null;

    /// <summary>Host, Exiled-Prefix.</summary>
    public static void OnExiled(PlayerControl player)
    {
        if (!Players.IsHost || _winner != null || RoleState.Get(player.PlayerId) != RoleId.Jester) return;
        var id = player.PlayerId;
        Net.Send(RpcIds.JesterWin, writer => writer.Write(id));
        OnWin(id);
        SpecialRolesPlugin.Logger.LogInfo("Der Narr wurde rausgewählt – Spielende.");
        GameManager.Instance.RpcEndGame(WinReason, false);
    }

    /// <summary>Alle Clients (vom Host).</summary>
    public static void OnWin(byte playerId) => _winner = playerId;

    /// <summary>OnGameEnd-Prefix: Gewinner sichern, bevor die Spieldaten weg sind.</summary>
    public static void BeforeGameEnd()
    {
        _winnerData = null;
        if (_winner == null) return;
        var info = GameData.Instance != null ? GameData.Instance.GetPlayerById(_winner.Value) : null;
        if (info != null) _winnerData = new CachedPlayerData(info);
    }

    /// <summary>OnGameEnd-Postfix: nur der Narr hat gewonnen.</summary>
    public static void AfterGameEnd()
    {
        if (_winnerData == null) return;
        EndGameResult.CachedWinners.Clear();
        EndGameResult.CachedWinners.Add(_winnerData);
    }

    /// <summary>EndGameManager.SetEverythingUp-Postfix.</summary>
    public static void ShowEndScreen(EndGameManager manager)
    {
        if (_winnerData == null || manager.WinText == null) return;
        manager.WinText.text = "Der Narr gewinnt!";
        manager.WinText.color = Colors.Of(RoleId.Jester);
    }

    /// <summary>ExileController.Begin-Postfix: „X war der Narr.“</summary>
    public static string? ExileText(NetworkedPlayerInfo? exiled) =>
        exiled != null && RoleState.Get(exiled.PlayerId) == RoleId.Jester ? $"{exiled.PlayerName} war der Narr." : null;

    /// <summary>Neue Runde.</summary>
    public static void Reset()
    {
        _winner = null;
        _winnerData = null;
    }
}
