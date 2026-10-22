using System.Collections.Generic;
using SpecialRoles.Core;
using SpecialRoles.Game;
using UnityEngine;

namespace SpecialRoles.Roles;

/// <summary>
/// Bestatter (Impostor): hebt eine Leiche auf und trägt sie (langsamer) woanders hin. Alle Clients bewegen die Leiche
/// hinter ihm her; beim Ablegen schickt er die genaue Stelle. Vent, Meeting oder Tod lassen sie fallen.
/// </summary>
internal static class Undertaker
{
    // Alle Clients: Bestatter → getragene Leiche (PlayerId des Toten)
    private static readonly Dictionary<byte, byte> Carrying = new();

    public static readonly CarryButton Button = new();

    internal sealed class CarryButton : RoleButton
    {
        protected override string Label
        {
            get
            {
                var local = PlayerControl.LocalPlayer;
                return local != null && IsCarrying(local.PlayerId) ? "Ablegen" : "Tragen";
            }
        }
        protected override KeyCode Key => KeyCode.F;
        protected override float Cooldown => 3f;
        protected override bool CooldownAfterUse => false;
        protected override Sprite? Icon(HudManager hud) => hud.ReportButton != null ? hud.ReportButton.graphic.sprite : null;

        protected override bool HasTarget(PlayerControl local, PlayerControl? target) =>
            IsCarrying(local.PlayerId) || Bodies.Closest(local.GetTruePosition()) != null;

        protected override void Use(PlayerControl local, PlayerControl? target)
        {
            if (IsCarrying(local.PlayerId))
            {
                Drop(local);
                SetTimer(Cooldown);
                return;
            }
            var body = Bodies.Closest(local.GetTruePosition());
            if (body == null) return;
            var id = body.ParentId;
            Net.Send(RpcIds.DragBody, writer => writer.Write(id));
            OnDrag(local, id);
        }
    }

    public static bool IsCarrying(byte playerId) => Carrying.ContainsKey(playerId);

    /// <summary>Bestatter: Leiche an der aktuellen Stelle ablegen (auch automatisch beim Venten).</summary>
    private static void Drop(PlayerControl local)
    {
        if (!Carrying.TryGetValue(local.PlayerId, out var bodyId)) return;
        var position = local.GetTruePosition();
        Net.Send(RpcIds.DropBody, writer =>
        {
            writer.Write(bodyId);
            writer.Write(position.x);
            writer.Write(position.y);
        });
        OnDrop(local, bodyId, position);
    }

    public static void OnDrag(PlayerControl undertaker, byte bodyId)
    {
        if (RoleState.Get(undertaker.PlayerId) != RoleId.Undertaker || !Players.IsAlive(undertaker)) return;
        if (Carrying.ContainsValue(bodyId) || Bodies.ById(bodyId) == null) return;
        Carrying[undertaker.PlayerId] = bodyId;
    }

    public static void OnDrop(PlayerControl undertaker, byte bodyId, Vector2 position)
    {
        if (!Carrying.TryGetValue(undertaker.PlayerId, out var carried) || carried != bodyId) return;
        Carrying.Remove(undertaker.PlayerId);
        var body = Bodies.ById(bodyId);
        if (body != null) Bodies.MoveTo(body, position);
    }

    /// <summary>Jeden Frame (alle Clients): getragene Leichen folgen ihrem Bestatter.</summary>
    public static void Update(PlayerControl local)
    {
        if (Carrying.Count == 0) return;
        if (Carrying.ContainsKey(local.PlayerId) && local.inVent) Drop(local);

        var stop = new List<byte>();
        foreach (var (undertakerId, bodyId) in Carrying)
        {
            var undertaker = Players.ById(undertakerId);
            var body = Bodies.ById(bodyId);
            if (!Players.IsAlive(undertaker) || body == null || Players.InMeeting)
            {
                stop.Add(undertakerId);
                continue;
            }
            Bodies.MoveTo(body, undertaker!.GetTruePosition());
        }
        foreach (var id in stop) Carrying.Remove(id);
    }

    public static void Reset() => Carrying.Clear();
}
