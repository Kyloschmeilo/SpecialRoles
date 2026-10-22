using System.Collections.Generic;
using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpecialRoles.Roles;

/// <summary>
/// Kameramann (Crew): stellt bis zu zwei eigene Kameras auf (nur er sieht sie) und schaut durch sie hindurch.
/// Beim Durchschauen wandern Sicht (LightSource) und Kamera zur Kamera, der Spieler bleibt stehen.
/// </summary>
internal static class Cameraman
{
    private const int MaxCams = 2;

    private static readonly List<SpriteRenderer> Cams = new();
    private static int _viewing = -1;
    private static float _viewUntil;
    private static Transform? _lightParent;
    private static Vector3 _lightPosition;

    public static readonly PlaceButton Place = new();
    public static readonly ViewButton View = new();

    internal sealed class PlaceButton : RoleButton
    {
        protected override string Label => $"Kamera ({MaxCams - Cams.Count})";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => 5f;
        protected override bool Visible(PlayerControl local) => Players.IsAlive(local) && Cams.Count < MaxCams;
        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => _viewing < 0;

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            Cams.Add(Sprites.Spawn("Kamera", Sprites.Square, local.GetTruePosition(), 0.3f, Colors.Of(RoleId.Cameraman, 0.8f)));
            Notify.Show(Colors.Paint(RoleId.Cameraman, $"Kamera {Cams.Count} aufgestellt."));
        }
    }

    internal sealed class ViewButton : RoleButton
    {
        protected override string Label => _viewing < 0 ? "Ansehen" : _viewing + 1 < Cams.Count ? "Nächste" : "Zurück";
        protected override KeyCode Key => KeyCode.G;
        protected override float Cooldown => RoleState.Settings.Get(SettingId.CameramanCooldown);
        protected override bool CooldownAfterUse => false;
        protected override bool Visible(PlayerControl local) => Players.IsAlive(local) && Cams.Count > 0;
        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => true;

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            if (_viewing < 0) StartViewing(local, 0);
            else if (_viewing + 1 < Cams.Count) StartViewing(local, _viewing + 1);
            else StopViewing();
        }
    }

    private static void StartViewing(PlayerControl local, int index)
    {
        var cam = Cams[index];
        var light = local.lightSource;
        var hud = HudManager.Instance;
        if (cam == null || light == null || hud == null) return;
        if (_viewing < 0)
        {
            _lightParent = light.transform.parent;
            _lightPosition = light.transform.localPosition;
            _viewUntil = Time.time + RoleState.Settings.Get(SettingId.CameramanDuration);
        }
        _viewing = index;
        light.transform.SetParent(null);
        var position = cam.transform.position;
        light.transform.position = new Vector3(position.x, position.y, light.transform.position.z);
        hud.PlayerCam.SetTarget(light);
        local.moveable = false;
    }

    private static void StopViewing()
    {
        if (_viewing < 0) return;
        _viewing = -1;
        View.SetTimer(RoleState.Settings.Get(SettingId.CameramanCooldown));
        var local = PlayerControl.LocalPlayer;
        if (local == null) return;
        var light = local.lightSource;
        if (light != null)
        {
            light.transform.SetParent(_lightParent != null ? _lightParent : local.transform);
            light.transform.localPosition = _lightPosition;
        }
        var hud = HudManager.Instance;
        if (hud != null && hud.PlayerCam != null) hud.PlayerCam.SetTarget(local);
        local.moveable = true;
    }

    /// <summary>Jeden Frame: Durchschauen nach Ablauf, im Meeting, beim Tod oder Rollenwechsel beenden.</summary>
    public static void Update(PlayerControl local)
    {
        if (_viewing >= 0 && (Time.time >= _viewUntil || Players.InMeeting || !Players.IsAlive(local) || RoleState.Local != RoleId.Cameraman))
            StopViewing();
        else if (_viewing >= 0)
            local.moveable = false;
    }

    public static void Reset()
    {
        StopViewing();
        foreach (var cam in Cams)
            if (cam != null) Object.Destroy(cam.gameObject);
        Cams.Clear();
    }
}
