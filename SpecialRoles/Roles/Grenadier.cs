using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpecialRoles.Roles;

/// <summary>
/// Grenadier (Impostor): zündet eine Blendgranate bei sich. Wer im Radius ist, sieht für ein paar Sekunden fast nur
/// Weiß; Impostor nur leicht, damit sie die Lage sehen. Jeder Client blendet sich selbst.
/// </summary>
internal static class Grenadier
{
    private const float ImpostorAlpha = 0.25f;
    private const float CrewAlpha = 0.97f;

    private static SpriteRenderer? _overlay;
    private static float _until;
    private static float _alpha;

    public static readonly GrenadeButton Button = new();

    internal sealed class GrenadeButton : RoleButton
    {
        protected override string Label => "Granate";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => RoleState.Settings.Get(SettingId.GrenadierCooldown);
        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => true;

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            var position = local.GetTruePosition();
            Net.Send(RpcIds.Grenade, writer =>
            {
                writer.Write(position.x);
                writer.Write(position.y);
            });
            OnGrenade(local, position);
        }
    }

    /// <summary>Alle Clients: bin ich im Radius, werde ich geblendet.</summary>
    public static void OnGrenade(PlayerControl grenadier, Vector2 position)
    {
        if (RoleState.Get(grenadier.PlayerId) != RoleId.Grenadier || !Players.IsAlive(grenadier)) return;
        var local = PlayerControl.LocalPlayer;
        if (!Players.IsAlive(local) || Players.InMeeting) return;
        if (Vector2.Distance(local!.GetTruePosition(), position) > RoleState.Settings.Get(SettingId.GrenadierRadius)) return;

        var overlay = Overlay();
        if (overlay == null) return;
        _alpha = Players.IsImpostor(local) ? ImpostorAlpha : CrewAlpha;
        _until = Time.time + RoleState.Settings.Get(SettingId.GrenadierDuration);
        overlay.color = new Color(1f, 1f, 1f, _alpha);
        overlay.gameObject.SetActive(true);
        overlay.enabled = true;
    }

    /// <summary>Jeden Frame: Blendung in der letzten Sekunde ausblenden.</summary>
    public static void Update()
    {
        if (_overlay == null || !_overlay.gameObject.activeSelf) return;
        var left = _until - Time.time;
        if (left <= 0f || Players.InMeeting)
        {
            _overlay.gameObject.SetActive(false);
            return;
        }
        _overlay.color = new Color(1f, 1f, 1f, _alpha * Mathf.Clamp01(left));
    }

    private static SpriteRenderer? Overlay()
    {
        if (_overlay != null) return _overlay;
        var hud = HudManager.Instance;
        if (hud == null || hud.FullScreen == null) return null;
        var overlay = Object.Instantiate(hud.FullScreen, hud.FullScreen.transform.parent);
        overlay.name = "SpecialRoles_Blendung";
        _overlay = overlay;
        return overlay;
    }

    public static void Reset()
    {
        if (_overlay != null) Object.Destroy(_overlay.gameObject);
        _overlay = null;
        _until = 0f;
    }
}
