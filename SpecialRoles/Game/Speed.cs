using SpecialRoles.Core;
using SpecialRoles.Roles;

namespace SpecialRoles.Game;

/// <summary>
/// Lauftempo des eigenen Spielers: Flash schneller, Bestatter mit Leiche langsamer. Bewegung rechnet jeder Spieler
/// selbst, die anderen sehen das Tempo.
/// </summary>
internal static class Speed
{
    private static PlayerPhysics? _physics;
    private static float _baseSpeed;

    public static void Update(PlayerControl local)
    {
        var physics = local.MyPhysics;
        if (physics == null) return;
        var factor = 1f;
        if (RoleState.LocalHas(Modifier.Flash)) factor *= RoleState.Settings.FlashSpeed;
        if (Undertaker.IsCarrying(local.PlayerId)) factor *= RoleState.Settings.Get(SettingId.UndertakerSpeed);
        if (factor == 1f && _physics == null) return;

        if (_physics != physics)
        {
            Reset();
            _physics = physics;
            _baseSpeed = physics.Speed;
        }
        physics.Speed = _baseSpeed * factor;
        if (factor == 1f) Reset();
    }

    public static void Reset()
    {
        if (_physics != null) _physics.Speed = _baseSpeed;
        _physics = null;
    }
}
