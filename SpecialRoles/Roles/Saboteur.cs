using System.Linq;
using SpecialRoles.Core;
using SpecialRoles.Game;

namespace SpecialRoles.Roles;

/// <summary>
/// Saboteur (Impostor): solange ein Saboteur lebt, ist der Sabotage-Cooldown aller Impostor kürzer. Der Host verkürzt
/// den Timer des Sabotage-Systems jedes Mal, wenn er neu startet (das Spiel schickt ihn an alle).
/// </summary>
internal static class Saboteur
{
    private static float _lastTimer;

    /// <summary>Host, jeden Frame.</summary>
    public static void HostUpdate()
    {
        if (!RoleState.PlayersWith(RoleId.Saboteur).Any()) return;
        var ship = ShipStatus.Instance;
        if (ship == null || !ship.Systems.ContainsKey(SystemTypes.Sabotage)) return;
        var sabotage = ship.Systems[SystemTypes.Sabotage].TryCast<SabotageSystemType>();
        if (sabotage == null) return;

        var timer = sabotage.Timer;
        if (timer > _lastTimer + 1f && SaboteurAlive())
        {
            timer *= RoleState.Settings.Get(SettingId.SaboteurFactor);
            sabotage.Timer = timer;
            sabotage.IsDirty = true;
        }
        _lastTimer = timer;
    }

    private static bool SaboteurAlive() =>
        RoleState.PlayersWith(RoleId.Saboteur).Any(id => Players.IsAlive(Players.ById(id)));

    public static void Reset() => _lastTimer = 0f;
}
