using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Tarner (Impostor): für ein paar Sekunden sehen alle Spieler gleich aus (grau, ohne Namen, Hut, Skin, Visier, Haustier).
/// Nur das Aussehen ändert sich (RawSet*), die echten Daten nicht – danach stellt jeder Client das Outfit wieder her.
/// Endet spätestens beim Meeting.
/// </summary>
internal static class Camouflager
{
    private const int Gray = 15;

    private static float _until = -1f;
    private static float _nextApply;
    private static bool _applied;

    public static bool Active => _until >= 0f;

    public static readonly CamoButton Button = new();

    internal sealed class CamoButton : RoleButton
    {
        protected override string Label => "Tarnen";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => RoleState.Settings.Get(SettingId.CamouflagerCooldown);
        protected override bool HasTarget(PlayerControl local, PlayerControl? target) => !Active;

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            Net.Send(RpcIds.Camouflage, _ => { });
            OnCamouflage(local);
        }
    }

    public static void OnCamouflage(PlayerControl camouflager)
    {
        if (RoleState.Get(camouflager.PlayerId) != RoleId.Camouflager || !Players.IsAlive(camouflager) || Players.InMeeting) return;
        _until = Time.time + RoleState.Settings.Get(SettingId.CamouflagerDuration);
        _nextApply = 0f;
        _applied = false;
    }

    /// <summary>Jeden Frame (alle Clients).</summary>
    public static void Update()
    {
        if (!Active) return;
        if (Time.time >= _until || Players.InMeeting)
        {
            End();
            return;
        }
        // jede Sekunde neu, falls das Spiel zwischendurch ein Outfit setzt (Gestaltwandler, Wiederbeleben …)
        if (Time.time < _nextApply) return;
        _nextApply = Time.time + 1f;
        foreach (var player in Players.All())
        {
            if (player.Data == null || player.Data.IsDead) continue;
            player.RawSetColor(Gray);
            player.RawSetName("");
            player.RawSetHat(CosmeticsLayer.EMPTY_HAT_ID, Gray);
            player.RawSetSkin(CosmeticsLayer.EMPTY_SKIN_ID, Gray);
            player.RawSetVisor(CosmeticsLayer.EMPTY_VISOR_ID, Gray);
            if (!_applied) player.RawSetPet(CosmeticsLayer.EMPTY_PET_ID, Gray);
        }
        _applied = true;
    }

    private static void End()
    {
        _until = -1f;
        if (!_applied) return;
        _applied = false;
        foreach (var player in Players.All())
            if (player.Data != null && player.CurrentOutfit != null)
                player.RawSetOutfit(player.CurrentOutfit, player.CurrentOutfitType);
    }

    public static void Reset()
    {
        if (Active) End();
        _until = -1f;
    }
}
