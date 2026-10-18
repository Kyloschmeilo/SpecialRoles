using System;
using System.Linq;
using Hazel;
using SpecialRoles.Core;
using SpecialRoles.Roles;

namespace SpecialRoles.Game;

/// <summary>Eigene RPCs über das PlayerControl des Absenders (<see cref="RpcIds"/>).</summary>
internal static class Net
{
    public static bool IsOurs(byte callId) => callId is RpcIds.SetRoles or RpcIds.SheriffShoot or RpcIds.CleanBody;

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
        switch (callId)
        {
            case RpcIds.SetRoles:
                if (sender.OwnerId != client.HostId)
                {
                    SpecialRolesPlugin.Logger.LogWarning("Rollen-Daten nicht vom Host – ignoriert.");
                    return;
                }
                RoleState.Apply(RoleSync.Decode(reader.ReadBytesAndSize().ToArray()));
                break;
            case RpcIds.SheriffShoot:
                var target = reader.ReadByte();
                if (client.AmHost) Sheriff.HostResolve(sender, target);
                break;
            case RpcIds.CleanBody:
                Janitor.OnCleanReceived(sender, reader.ReadByte());
                break;
        }
    }
}
