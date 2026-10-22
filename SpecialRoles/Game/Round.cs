using SpecialRoles.Core;
using SpecialRoles.Roles;
using UnityEngine;

namespace SpecialRoles.Game;

/// <summary>
/// Ablauf einer Runde: läuft jeden Frame (HudManager.Update), erkennt Meeting-Beginn/-Ende und setzt am Spielende
/// alles zurück. Jeder Schritt fängt eigene Fehler ab.
/// </summary>
internal static class Round
{
    /// <summary>So lange nach einem Meeting zählen Fallen nicht (alle wurden zum Tisch versetzt).</summary>
    private const float AfterMeeting = 3f;

    private static bool _inMeeting;
    private static float _meetingEndedAt = -100f;

    /// <summary>Im Meeting oder kurz danach.</summary>
    public static bool JustTeleported => _inMeeting || Time.time - _meetingEndedAt < AfterMeeting;

    public static void Tick()
    {
        var hud = HudManager.Instance;
        var local = PlayerControl.LocalPlayer;
        if (hud == null || local == null || local.Data == null) return;

        var inMeeting = Players.InMeeting;
        if (inMeeting != _inMeeting)
        {
            _inMeeting = inMeeting;
            if (inMeeting) Safe.Run(OnMeetingStart);
            else Safe.Run(OnMeetingEnd);
        }

        Safe.Run(Notify.Update);
        Safe.Run(Footprints.Update);
        if (RoleState.Local is RoleId.Tracker or RoleId.Medium) Safe.Run(Footprints.Record);
        Safe.Run(() => Speed.Update(local));

        Safe.Run(Camouflager.Update);
        Safe.Run(() => Undertaker.Update(local));
        Safe.Run(() => Medic.Update(local));
        Safe.Run(() => Snitch.Update(local));
        Safe.Run(() => Trapper.Update(local));
        Safe.Run(() => Bomber.Update(local));
        Safe.Run(() => Cameraman.Update(local));
        Safe.Run(Hacker.Update);
        Safe.Run(Grenadier.Update);
        Safe.Run(() => Vampire.UpdateLabel(hud));
        if (Players.IsHost)
        {
            Safe.Run(Vampire.HostUpdate);
            Safe.Run(Saboteur.HostUpdate);
        }
        if (MeetingHud.Instance != null) Safe.Run(MeetingTags.Update);

        Safe.Run(() => NameTag.Update(local));
        Hud.TickButtons(hud, local);

        Safe.Run(Arrows.EndFrame);
        Safe.Run(Markers.EndFrame);
    }

    private static void OnMeetingStart()
    {
        Safe.Run(Camouflager.Reset);
        Safe.Run(Bomber.OnMeetingStart);
        Safe.Run(Warlock.OnMeetingStart);
        Safe.Run(Blackmailer.OnMeetingStart);
        Safe.Run(Grenadier.Reset);
    }

    private static void OnMeetingEnd()
    {
        _meetingEndedAt = Time.time;
        Safe.Run(Blackmailer.OnMeetingEnd);
        Safe.Run(Mechanic.OnMeetingEnd);
        Safe.Run(Transporter.OnMeetingEnd);
    }

    /// <summary>Spielende/Verlassen: alles zurücksetzen (der Narr-Sieg bleibt für den Endbildschirm erhalten).</summary>
    public static void EndGame()
    {
        Safe.Run(Hud.Reset);
        Safe.Run(Speed.Reset);
        Safe.Run(Notify.Reset);
        Safe.Run(Arrows.Reset);
        Safe.Run(Markers.Reset);
        Safe.Run(Footprints.Reset);
        Safe.Run(Deaths.Reset);
        Safe.Run(Attack.Reset);

        Safe.Run(Sheriff.Reset);
        Safe.Run(Medic.Reset);
        Safe.Run(Snitch.Reset);
        Safe.Run(Hacker.Reset);
        Safe.Run(Trapper.Reset);
        Safe.Run(Veteran.Reset);
        Safe.Run(Mechanic.Reset);
        Safe.Run(Transporter.Reset);
        Safe.Run(Oracle.Reset);
        Safe.Run(Cameraman.Reset);
        Safe.Run(Undertaker.Reset);
        Safe.Run(Vampire.Reset);
        Safe.Run(Bomber.Reset);
        Safe.Run(Grenadier.Reset);
        Safe.Run(Miner.Reset);
        Safe.Run(Blackmailer.Reset);
        Safe.Run(Camouflager.Reset);
        Safe.Run(Warlock.Reset);
        Safe.Run(Saboteur.Reset);

        Safe.Run(RoleState.Reset);
        _inMeeting = false;
        _meetingEndedAt = -100f;
    }

    /// <summary>Neue Runde (Rollen vom Host kommen): auch den Narr-Sieg der letzten Runde vergessen.</summary>
    public static void NewGame()
    {
        EndGame();
        Safe.Run(Jester.Reset);
    }
}
