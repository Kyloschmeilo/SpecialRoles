using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Erpresser (Impostor, 2× pro Spiel): der erpresste Spieler darf im nächsten Meeting nicht schreiben (der Chat-Knopf
/// sendet bei ihm nichts) und alle sehen „erpresst“ hinter seinem Namen – reden im Voice soll er dann auch nicht.
/// Abstimmen darf er.
/// </summary>
internal static class Blackmailer
{
    private const int UsesPerGame = 2;

    // Alle Clients: wer im nächsten Meeting schweigen muss (immer nur einer)
    private static byte? _target;
    private static bool _meetingStarted;
    private static int _used;
    private static float _warnedAt = -10f;

    public static readonly BlackmailButton Button = new();

    internal sealed class BlackmailButton : RoleButton
    {
        protected override string Label => $"Erpressen ({UsesPerGame - _used})";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => RoleState.Settings.Get(SettingId.BlackmailerCooldown);
        protected override bool Visible(PlayerControl local) => Players.IsAlive(local) && _used < UsesPerGame;

        protected override PlayerControl? FindTarget(PlayerControl local) =>
            _target != null ? null : Players.ClosestTarget(local, Players.KillDistance(), p => !Players.IsImpostor(p));

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            if (target == null || _used >= UsesPerGame) return;
            _used++;
            var id = target.PlayerId;
            Net.Send(RpcIds.Blackmail, writer => writer.Write(id));
            OnBlackmail(local, id);
            Notify.Show(Players.ColoredName(id) + Colors.Paint(RoleId.Blackmailer, " muss im nächsten Meeting schweigen."));
        }
    }

    public static void OnBlackmail(PlayerControl blackmailer, byte targetId)
    {
        if (RoleState.Get(blackmailer.PlayerId) != RoleId.Blackmailer || !Players.IsAlive(blackmailer)) return;
        if (Players.InMeeting) return;
        _target = targetId;
    }

    /// <summary>Darf ich gerade nicht schreiben? (Chat-Prefix, nur der eigene Client)</summary>
    public static bool LocalSilenced()
    {
        var local = PlayerControl.LocalPlayer;
        return _meetingStarted && local != null && _target == local.PlayerId && Players.IsAlive(local) && MeetingHud.Instance != null;
    }

    /// <summary>Chat-Prefix: Senden blocken und (höchstens alle 2 s) lokal erklären.</summary>
    public static void WarnSilenced()
    {
        if (Time.time - _warnedAt < 2f) return;
        _warnedAt = Time.time;
        HudManager.Instance?.Chat?.AddChatWarning("Du wurdest erpresst und darfst in diesem Meeting nicht schreiben.");
    }

    public static string MeetingTag(byte playerId) =>
        _meetingStarted && _target == playerId ? Colors.Paint(RoleId.Blackmailer, "erpresst") : "";

    public static void OnMeetingStart()
    {
        _meetingStarted = _target != null && Players.IsAlive(Players.ById(_target.Value));
        var local = PlayerControl.LocalPlayer;
        if (_meetingStarted && local != null && local.PlayerId == _target)
        {
            Notify.Show(Colors.Paint(RoleId.Blackmailer, "Du wurdest erpresst: in diesem Meeting nicht schreiben und nicht reden!"), 10f);
            HudManager.Instance?.Chat?.AddChatWarning("Du wurdest erpresst: in diesem Meeting nicht schreiben und nicht reden.");
        }
    }

    public static void OnMeetingEnd()
    {
        _target = null;
        _meetingStarted = false;
    }

    public static void Reset()
    {
        _target = null;
        _meetingStarted = false;
        _used = 0;
    }
}
