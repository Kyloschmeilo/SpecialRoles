using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>Spurenleser (Crew): sieht kurz die Fußspuren aller Spieler der letzten Sekunden, in ihren Farben.</summary>
internal static class Tracker
{
    private const float Seconds = 10f;

    public static readonly TrackButton Button = new();

    internal sealed class TrackButton : RoleButton
    {
        protected override string Label => "Spuren";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => RoleState.Settings.Get(SettingId.TrackerCooldown);
        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => true;

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            var now = Time.time;
            var duration = RoleState.Settings.Get(SettingId.TrackerDuration);
            var count = 0;
            foreach (var player in Players.All())
                if (player.PlayerId != local.PlayerId)
                    count += Footprints.Show(player.PlayerId, now - Seconds, now, Players.ColorOf(player.PlayerId), duration);
            if (count == 0) Notify.Show(Colors.Paint(RoleId.Tracker, "Gerade gibt es keine frischen Spuren."));
        }
    }
}
