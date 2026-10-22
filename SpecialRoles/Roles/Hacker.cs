using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>Hacker (Crew): öffnet von überall kurz die Admin-Karte (wie am Admin-Tisch, man kann dabei laufen).</summary>
internal static class Hacker
{
    private static float _closeAt = -1f;

    public static readonly AdminButton Button = new();

    internal sealed class AdminButton : RoleButton
    {
        protected override string Label => "Admin";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => RoleState.Settings.Get(SettingId.HackerCooldown);
        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => _closeAt < 0f;

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            var hud = HudManager.Instance;
            if (hud == null) return;
            if (MapBehaviour.Instance == null) hud.InitMap();
            var map = MapBehaviour.Instance;
            if (map == null) return;
            map.ShowCountOverlay(true, true, false);
            _closeAt = Time.time + RoleState.Settings.Get(SettingId.HackerDuration);
        }
    }

    public static void Update()
    {
        if (_closeAt < 0f || Time.time < _closeAt) return;
        Close();
    }

    private static void Close()
    {
        _closeAt = -1f;
        var map = MapBehaviour.Instance;
        if (map != null && map.IsOpen) map.Close();
    }

    public static void Reset()
    {
        if (_closeAt >= 0f) Close();
    }
}
