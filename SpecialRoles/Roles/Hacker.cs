using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Hacker (Crew): öffnet von überall kurz die Admin-Karte. Wie am Admin-Tisch steht er dabei still – beim Laufen
/// schließt das Spiel die Karte sonst sofort. Schließt er sie selbst, ist die Wirkung vorbei.
/// </summary>
internal static class Hacker
{
    /// <summary>So lange nach dem Öffnen zählt eine geschlossene Karte noch nicht als „selbst geschlossen“.</summary>
    private const float OpenGrace = 0.5f;

    private static float _openedAt = -1f;
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
            local.moveable = false;
            _openedAt = Time.time;
            _closeAt = Time.time + RoleState.Settings.Get(SettingId.HackerDuration);
        }
    }

    public static void Update()
    {
        if (_closeAt < 0f) return;
        var map = MapBehaviour.Instance;
        var closedByPlayer = Time.time - _openedAt > OpenGrace && (map == null || !map.IsOpen);
        if (Time.time >= _closeAt || closedByPlayer || !Players.IsAlive(PlayerControl.LocalPlayer)) Close();
    }

    private static void Close()
    {
        _closeAt = -1f;
        _openedAt = -1f;
        var local = PlayerControl.LocalPlayer;
        if (local != null) local.moveable = true;
        var map = MapBehaviour.Instance;
        if (map != null && map.IsOpen) map.Close();
    }

    public static void Reset()
    {
        if (_closeAt >= 0f) Close();
    }
}
