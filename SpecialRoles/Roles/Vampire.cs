using System.Collections.Generic;
using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Vampir (Impostor): sein Kill ist ein Biss – das Opfer stirbt erst nach der Verzögerung an Ort und Stelle (der Vampir
/// bleibt, wo er ist). Wird vorher ein Meeting einberufen, stirbt das Opfer sofort davor. Entscheidet der Host.
/// </summary>
internal static class Vampire
{
    private sealed record Bite(byte Vampire, byte Victim, float DiesAt);

    // Host: ausstehende Bisse
    private static readonly List<Bite> Pending = new();
    private static float _labelAt;

    /// <summary>CmdCheckMurder-Prefix des eigenen Spielers: false = statt Kill beißen.</summary>
    public static bool TryBite(PlayerControl vampire, PlayerControl target)
    {
        if (!vampire.AmOwner || RoleState.Get(vampire.PlayerId) != RoleId.Vampire || target == null) return false;
        var id = target.PlayerId;
        Net.Send(RpcIds.Bite, writer => writer.Write(id));
        OnBite(vampire, id);
        vampire.SetKillTimer(Players.KillCooldown());
        Notify.Show(Players.ColoredName(id) + Colors.Paint(RoleId.Vampire, " gebissen."));
        return true;
    }

    /// <summary>Alle Clients: merken, wer Täter ist (für das Medium); der Host plant den Tod.</summary>
    public static void OnBite(PlayerControl vampire, byte victimId)
    {
        if (RoleState.Get(vampire.PlayerId) != RoleId.Vampire || !Players.IsAlive(vampire)) return;
        var victim = Players.ById(victimId);
        if (!Players.IsAlive(victim) || Players.IsImpostor(victim!)) return;
        Deaths.BlameOn(victimId, vampire.PlayerId);
        if (!Players.IsHost) return;
        if (Vector2.Distance(vampire.GetTruePosition(), victim!.GetTruePosition()) > Players.KillDistance() + 1.5f) return;
        if (!Attack.Contact(vampire, victim)) return;
        Pending.Add(new Bite(vampire.PlayerId, victimId, Time.time + RoleState.Settings.Get(SettingId.VampireDelay)));
    }

    /// <summary>Host, jeden Frame.</summary>
    public static void HostUpdate()
    {
        if (Pending.Count == 0 || Players.InMeeting) return;
        var now = Time.time;
        for (var i = Pending.Count - 1; i >= 0; i--)
        {
            if (Pending[i].DiesAt > now) continue;
            var bite = Pending[i];
            Pending.RemoveAt(i);
            Execute(bite);
        }
    }

    /// <summary>Host, ReportDeadBody-Prefix: alle Gebissenen sterben vor dem Meeting.</summary>
    public static void BeforeMeeting()
    {
        var bites = new List<Bite>(Pending);
        Pending.Clear();
        foreach (var bite in bites) Execute(bite);
    }

    private static void Execute(Bite bite)
    {
        var victim = Players.ById(bite.Victim);
        if (!Players.IsAlive(victim)) return;
        if (Attack.Lethal(victim!)) Attack.Kill(victim!);
    }

    /// <summary>Eigener Kill-Knopf heißt „Beißen“.</summary>
    public static void UpdateLabel(HudManager hud)
    {
        if (RoleState.Local != RoleId.Vampire || hud.KillButton == null || Time.time < _labelAt) return;
        _labelAt = Time.time + 1f;
        hud.KillButton.OverrideText("Beißen");
    }

    public static void Reset()
    {
        Pending.Clear();
        _labelAt = 0f;
    }
}
