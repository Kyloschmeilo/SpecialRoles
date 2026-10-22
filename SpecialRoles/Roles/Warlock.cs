using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Hexenmeister (Impostor): verflucht einen Spieler (wenn der Kill bereit ist) und löst den Fluch später aus – dann
/// stirbt der nächste Nicht-Impostor neben dem Verfluchten, der Hexenmeister kann weit weg sein. Danach läuft der
/// Kill-Cooldown neu. Der Fluch verfällt im Meeting.
/// </summary>
internal static class Warlock
{
    private static byte? _cursed;

    public static readonly CurseButton Button = new();

    internal sealed class CurseButton : RoleButton
    {
        protected override string Label => _cursed == null ? "Verfluchen" : "Auslösen";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => Players.KillCooldown();
        protected override bool SharesKillTimer => _cursed == null;
        protected override bool CooldownAfterUse => false;

        protected override PlayerControl? FindTarget(PlayerControl local)
        {
            if (_cursed == null) return Players.ClosestTarget(local, Players.KillDistance());
            var cursed = Players.ById(_cursed.Value);
            if (!Players.IsAlive(cursed))
            {
                _cursed = null;
                return null;
            }
            return VictimNear(cursed!, local.PlayerId);
        }

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            if (target == null) return;
            if (_cursed == null)
            {
                _cursed = target.PlayerId;
                Notify.Show(Players.ColoredName(target.PlayerId) + Colors.Paint(RoleId.Warlock, " ist verflucht – löse den Fluch aus, wenn jemand neben ihm steht."));
                return;
            }
            var cursedId = _cursed.Value;
            _cursed = null;
            if (Players.IsHost) HostCurseKill(local, cursedId);
            else Net.Send(RpcIds.CurseKill, writer => writer.Write(cursedId));
            local.SetKillTimer(Players.KillCooldown());
        }
    }

    /// <summary>Nächster lebender Nicht-Impostor in Kill-Reichweite des Verfluchten (nicht der Hexenmeister selbst).</summary>
    private static PlayerControl? VictimNear(PlayerControl cursed, byte warlockId) =>
        Players.ClosestTo(cursed.GetTruePosition(), Players.KillDistance(),
            p => p.PlayerId != cursed.PlayerId && p.PlayerId != warlockId && !Players.IsImpostor(p));

    /// <summary>Host: der Verfluchte „tötet“ den Nächsten neben sich (ohne Teleport).</summary>
    public static void HostCurseKill(PlayerControl warlock, byte cursedId)
    {
        if (RoleState.Get(warlock.PlayerId) != RoleId.Warlock || !Players.IsAlive(warlock) || Players.InMeeting) return;
        var cursed = Players.ById(cursedId);
        if (!Players.IsAlive(cursed)) return;
        var victim = VictimNear(cursed!, warlock.PlayerId);
        if (victim == null) return;
        if (!Attack.Contact(cursed!, victim)) return;
        if (Attack.Lethal(victim)) Attack.Kill(victim);
    }

    public static void OnMeetingStart() => _cursed = null;

    public static void Reset() => _cursed = null;
}
