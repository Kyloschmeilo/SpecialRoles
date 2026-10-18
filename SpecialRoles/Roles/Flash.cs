using SpecialRoles.Core;
using SpecialRoles.Game;

namespace SpecialRoles.Roles;

/// <summary>Flash (Modifikator): läuft schneller. Bewegung rechnet jeder Spieler selbst, die anderen sehen das Tempo.</summary>
internal static class Flash
{
    private static PlayerPhysics? _physics;
    private static float _baseSpeed;

    public static void Update(PlayerControl local)
    {
        var physics = local.MyPhysics;
        if (physics == null) return;
        if (!RoleState.LocalHas(Modifier.Flash))
        {
            Reset();
            return;
        }
        if (_physics != physics)
        {
            Reset();
            _physics = physics;
            _baseSpeed = physics.Speed;
        }
        physics.Speed = _baseSpeed * RoleState.Settings.FlashSpeed;
    }

    public static void Reset()
    {
        if (_physics != null) _physics.Speed = _baseSpeed;
        _physics = null;
    }
}
