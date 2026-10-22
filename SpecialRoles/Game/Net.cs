using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using SpecialRoles.Core;
using SpecialRoles.Roles;
using UnityEngine;

namespace SpecialRoles.Game;

/// <summary>Eigene RPCs über das PlayerControl des Absenders (<see cref="RpcIds"/>).</summary>
internal static class Net
{
    public static bool IsOurs(byte callId) => RpcIds.IsOurs(callId);

    /// <summary>Schickt an alle anderen (der Absender bekommt seine eigene Nachricht nicht).</summary>
    public static void Send(byte callId, Action<MessageWriter> write)
    {
        var local = PlayerControl.LocalPlayer;
        var client = AmongUsClient.Instance;
        if (local == null || client == null) return;
        var writer = client.StartRpcImmediately(local.NetId, callId, SendOption.Reliable, -1);
        write(writer);
        client.FinishRpcImmediately(writer);
    }

    public static void Handle(PlayerControl sender, byte callId, MessageReader reader)
    {
        var client = AmongUsClient.Instance;
        if (client == null || sender == null) return;
        var fromHost = sender.OwnerId == client.HostId;
        var amHost = client.AmHost;
        switch (callId)
        {
            case RpcIds.SetRoles:
                if (!fromHost)
                {
                    SpecialRolesPlugin.Logger.LogWarning("Rollen-Daten nicht vom Host – ignoriert.");
                    return;
                }
                RoleState.Apply(RoleSync.Decode(reader.ReadBytesAndSize().ToArray()));
                break;
            case RpcIds.SheriffShoot:
            {
                var target = reader.ReadByte();
                if (amHost) Sheriff.HostResolve(sender, target);
                break;
            }
            case RpcIds.CleanBody:
                Janitor.OnCleanReceived(sender, reader.ReadByte());
                break;
            case RpcIds.MedicShield:
                Medic.OnShield(sender, reader.ReadByte());
                break;
            case RpcIds.ShieldBroken:
            {
                var target = reader.ReadByte();
                var kind = (Protection)reader.ReadByte();
                if (fromHost) Attack.OnProtected(target, kind);
                break;
            }
            case RpcIds.VeteranAlert:
                Veteran.OnAlert(sender);
                break;
            case RpcIds.Blackmail:
                Blackmailer.OnBlackmail(sender, reader.ReadByte());
                break;
            case RpcIds.Transport:
            {
                var a = reader.ReadByte();
                var b = reader.ReadByte();
                var posA = ReadVector(reader);
                var posB = ReadVector(reader);
                Transporter.OnTransport(sender, a, b, posA, posB);
                break;
            }
            case RpcIds.OracleChoose:
                Oracle.OnChoose(sender, reader.ReadByte());
                break;
            case RpcIds.ReviveRequest:
            {
                var body = reader.ReadByte();
                if (amHost) Altruist.HostRevive(sender, body);
                break;
            }
            case RpcIds.Revive:
            {
                var player = reader.ReadByte();
                var position = ReadVector(reader);
                var role = (RoleTypes)reader.ReadUInt16();
                if (fromHost) Altruist.OnRevive(player, position, role);
                break;
            }
            case RpcIds.CloseDoors:
            {
                var count = reader.ReadByte();
                var rooms = new List<SystemTypes>();
                for (var i = 0; i < count; i++) rooms.Add((SystemTypes)reader.ReadByte());
                if (amHost) Doorkeeper.HostClose(sender, rooms);
                break;
            }
            case RpcIds.DragBody:
                Undertaker.OnDrag(sender, reader.ReadByte());
                break;
            case RpcIds.DropBody:
            {
                var body = reader.ReadByte();
                Undertaker.OnDrop(sender, body, ReadVector(reader));
                break;
            }
            case RpcIds.Bite:
                Vampire.OnBite(sender, reader.ReadByte());
                break;
            case RpcIds.PlantBomb:
                Bomber.OnPlant(sender, reader.ReadByte());
                break;
            case RpcIds.Grenade:
                Grenadier.OnGrenade(sender, ReadVector(reader));
                break;
            case RpcIds.MinerVent:
            {
                var id = reader.ReadInt32();
                Miner.OnVent(sender, id, ReadVector(reader));
                break;
            }
            case RpcIds.Camouflage:
                Camouflager.OnCamouflage(sender);
                break;
            case RpcIds.CurseKill:
            {
                var cursed = reader.ReadByte();
                if (amHost) Warlock.HostCurseKill(sender, cursed);
                break;
            }
            case RpcIds.JesterWin:
            {
                var jester = reader.ReadByte();
                if (fromHost) Jester.OnWin(jester);
                break;
            }
            case RpcIds.Remember:
            {
                var body = reader.ReadByte();
                if (amHost) Amnesiac.HostRemember(sender, body);
                break;
            }
            case RpcIds.RoleChange:
            {
                var player = reader.ReadByte();
                var role = reader.ReadByte();
                var body = reader.ReadByte();
                if (fromHost) Amnesiac.OnRoleChange(player, RoleCatalog.IsKnown(role) ? (RoleId)role : RoleId.None, body);
                break;
            }
            case RpcIds.MechanicFix:
                if (amHost) Mechanic.HostRepair(sender);
                break;
            case RpcIds.KillBlocked:
            {
                var killer = reader.ReadByte();
                reader.ReadByte(); // Ziel – für spätere Anzeigen
                if (fromHost) Attack.OnKillBlocked(killer);
                break;
            }
        }
    }

    private static Vector2 ReadVector(MessageReader reader)
    {
        var x = reader.ReadSingle();
        var y = reader.ReadSingle();
        return new Vector2(x, y);
    }
}
