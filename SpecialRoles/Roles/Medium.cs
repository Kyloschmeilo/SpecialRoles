using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Medium (Crew): an einer frischen Leiche sieht es den Weg des Mörders kurz vor der Tat (graue Spuren, ohne Farbe).
/// Den Weg hebt <see cref="Deaths"/> beim Tod auf; dafür zeichnet das Medium die Spuren aller ständig mit.
/// </summary>
internal static class Medium
{
    private const float MaxAge = 60f;
    private const float ShowSeconds = 8f;

    public static readonly ReadButton Button = new();

    internal sealed class ReadButton : RoleButton
    {
        protected override string Label => "Lesen";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => RoleState.Settings.Get(SettingId.MediumCooldown);
        protected override Sprite? Icon(HudManager hud) => hud.ReportButton != null ? hud.ReportButton.graphic.sprite : null;
        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => FreshBody(local) != null;

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            var body = FreshBody(local);
            if (body == null) return;
            var death = Deaths.Of(body.ParentId);
            if (death?.Killer == null || death.Path.Count == 0)
            {
                Notify.Show(Colors.Paint(RoleId.Medium, "Hier ist keine Spur zu finden."));
                return;
            }
            Footprints.ShowPath(death.Path, new Color(0.75f, 0.75f, 0.75f), ShowSeconds);
            Notify.Show(Colors.Paint(RoleId.Medium, "Du siehst, woher der Mörder kam."));
        }
    }

    private static DeadBody? FreshBody(PlayerControl local) =>
        Bodies.Closest(local.GetTruePosition(), Bodies.Range, body =>
        {
            var death = Deaths.Of(body.ParentId);
            return death != null && Time.time - death.Time <= MaxAge;
        });
}
