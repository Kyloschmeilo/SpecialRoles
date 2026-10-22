using System.Collections.Generic;
using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Bomber (Impostor): klebt einem Spieler eine Bombe an (teilt sich den Cooldown mit dem Kill). Nach der Verzögerung
/// explodiert sie: der Träger und alle Nicht-Impostor im Radius sterben. Kommt vorher ein Meeting, ist sie entschärft.
/// Der Träger merkt nichts; der Bomber sieht einen Ring um ihn.
/// </summary>
internal static class Bomber
{
    private sealed record Bomb(byte Bomber, byte Carrier, float ExplodesAt);

    // Alle Clients (nur der Host lässt sie explodieren)
    private static readonly List<Bomb> Bombs = new();

    public static readonly BombButton Button = new();

    internal sealed class BombButton : RoleButton
    {
        protected override string Label => "Bombe";
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => Players.KillCooldown();
        protected override bool SharesKillTimer => true;

        protected override PlayerControl? FindTarget(PlayerControl local) =>
            Players.ClosestTarget(local, Players.KillDistance(), p => !Players.IsImpostor(p) && !HasBomb(p.PlayerId));

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            if (target == null) return;
            var id = target.PlayerId;
            Net.Send(RpcIds.PlantBomb, writer => writer.Write(id));
            OnPlant(local, id);
            local.SetKillTimer(Players.KillCooldown());
        }
    }

    private static bool HasBomb(byte playerId) => Bombs.Exists(b => b.Carrier == playerId);

    public static void OnPlant(PlayerControl bomber, byte carrierId)
    {
        if (RoleState.Get(bomber.PlayerId) != RoleId.Bomber || !Players.IsAlive(bomber)) return;
        var carrier = Players.ById(carrierId);
        if (!Players.IsAlive(carrier) || Players.IsImpostor(carrier!)) return;
        if (Players.IsHost)
        {
            if (Vector2.Distance(bomber.GetTruePosition(), carrier!.GetTruePosition()) > Players.KillDistance() + 1.5f) return;
            if (!Attack.Contact(bomber, carrier)) return;
        }
        Deaths.BlameOn(carrierId, bomber.PlayerId);
        Bombs.Add(new Bomb(bomber.PlayerId, carrierId, Time.time + RoleState.Settings.Get(SettingId.BomberDelay)));
    }

    /// <summary>Jeden Frame: Ring für den Bomber, Explosion beim Host.</summary>
    public static void Update(PlayerControl local)
    {
        if (Bombs.Count == 0) return;
        var now = Time.time;
        for (var i = Bombs.Count - 1; i >= 0; i--)
        {
            var bomb = Bombs[i];
            var carrier = Players.ById(bomb.Carrier);
            if (!Players.IsAlive(carrier))
            {
                Bombs.RemoveAt(i);
                continue;
            }
            if (local.PlayerId == bomb.Bomber)
                Markers.Ring($"bomb:{bomb.Carrier}", carrier!, Colors.Of(RoleId.Bomber, 0.9f), 0.7f);
            if (bomb.ExplodesAt > now) continue;
            Bombs.RemoveAt(i);
            if (Players.IsHost) Explode(carrier!);
        }
    }

    private static void Explode(PlayerControl carrier)
    {
        var center = carrier.GetTruePosition();
        var radius = RoleState.Settings.Get(SettingId.BomberRadius);
        var victims = new List<PlayerControl>();
        foreach (var player in Players.Alive())
            if (!Players.IsImpostor(player) && !player.inVent && Vector2.Distance(center, player.GetTruePosition()) <= radius)
                victims.Add(player);
        SpecialRolesPlugin.Logger.LogInfo($"Bombe explodiert: {victims.Count} Spieler im Radius.");
        foreach (var victim in victims)
            if (Attack.Lethal(victim))
                Attack.Kill(victim);
    }

    /// <summary>Meeting: alle Bomben sind entschärft.</summary>
    public static void OnMeetingStart() => Bombs.Clear();

    public static void Reset() => Bombs.Clear();
}
