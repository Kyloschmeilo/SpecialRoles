using System.Collections.Generic;
using System.Security.Cryptography;
using SpecialRoles.Core;
using SpecialRoles.Roles;

namespace SpecialRoles.Game;

/// <summary>
/// Host: alle Angriffe (normaler Kill, Sheriff-Schuss, Biss, Bombe, Fluch) laufen hier durch, damit Veteran, Medic-Schild,
/// Glückspilz und Armor überall gleich greifen.
/// </summary>
internal static class Attack
{
    // Host: wer seinen einmaligen Schutz schon verbraucht hat
    private static readonly HashSet<byte> LuckUsed = new();
    private static readonly HashSet<byte> ArmorUsed = new();

    /// <summary>
    /// Der Angreifer berührt das Ziel (Kill, Schuss, Biss, Bombe ankleben …). Ist das Ziel ein wachsamer Veteran,
    /// stirbt der Angreifer. false = Angriff hat nicht stattgefunden.
    /// </summary>
    public static bool Contact(PlayerControl attacker, PlayerControl target)
    {
        if (!Players.IsAlive(attacker) || !Players.IsAlive(target)) return false;
        if (Veteran.IsAlert(target.PlayerId) && attacker.PlayerId != target.PlayerId)
        {
            SpecialRolesPlugin.Logger.LogInfo("Veteran war wachsam – der Angreifer stirbt.");
            if (Lethal(attacker)) Kill(attacker);
            return false;
        }
        return true;
    }

    /// <summary>Gleich stirbt target: true = stirbt wirklich, false = Schild/Glück/Armor hat es verhindert.</summary>
    public static bool Lethal(PlayerControl target)
    {
        if (!Players.IsAlive(target)) return false;
        var id = target.PlayerId;
        if (Medic.TryBlock(id)) return false;
        if (RoleState.Get(id) == RoleId.Lucky && !LuckUsed.Contains(id)
            && RandomNumberGenerator.GetInt32(100) < RoleState.Settings.GetInt(SettingId.LuckyChance))
        {
            LuckUsed.Add(id);
            SendProtected(id, Protection.Luck);
            return false;
        }
        if (RoleState.Get(id) == RoleId.Armor && ArmorUsed.Add(id))
        {
            SendProtected(id, Protection.Armor);
            return false;
        }
        return true;
    }

    /// <summary>Tötet ohne Teleport: der Spieler „stirbt an Ort und Stelle“ (Mörder = er selbst).</summary>
    public static void Kill(PlayerControl target) => target.RpcMurderPlayer(target, true);

    /// <summary>Host: allen sagen, dass ein Angriff abgewehrt wurde.</summary>
    public static void SendProtected(byte targetId, Protection kind)
    {
        SpecialRolesPlugin.Logger.LogInfo($"Angriff abgewehrt ({kind}).");
        Net.Send(RpcIds.ShieldBroken, writer =>
        {
            writer.Write(targetId);
            writer.Write((byte)kind);
        });
        OnProtected(targetId, kind);
    }

    /// <summary>
    /// Host: ein normaler Kill wurde abgewehrt. Den Kill-Timer führt der Client des Angreifers selbst, darum geht es an
    /// alle und nur der Angreifer setzt ihn (RpcMurderPlayer mit „nicht geklappt“ macht im Spiel gar nichts).
    /// </summary>
    public static void SendKillBlocked(PlayerControl killer, PlayerControl target)
    {
        Net.Send(RpcIds.KillBlocked, writer =>
        {
            writer.Write(killer.PlayerId);
            writer.Write(target.PlayerId);
        });
        OnKillBlocked(killer.PlayerId);
    }

    /// <summary>Alle Clients: der Angreifer bekommt den vollen Kill-Cooldown und erfährt nur, dass es nicht geklappt hat.</summary>
    public static void OnKillBlocked(byte killerId)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.PlayerId != killerId) return;
        local.SetKillTimer(Players.KillCooldown());
        Notify.Show("<color=#FF4040>Dein Kill hat nicht geklappt.</color>");
    }

    /// <summary>Alle Clients.</summary>
    public static void OnProtected(byte targetId, Protection kind)
    {
        var local = PlayerControl.LocalPlayer;
        switch (kind)
        {
            case Protection.MedicShield:
                Medic.OnBroken(targetId);
                break;
            case Protection.Luck when local != null && local.PlayerId == targetId:
                Notify.Show(Colors.Paint(RoleId.Lucky, "Glück gehabt! Ein Angriff auf dich ist danebengegangen."));
                break;
            case Protection.Armor when local != null && local.PlayerId == targetId:
                Notify.Show(Colors.Paint(RoleId.Armor, "Deine Rüstung hat einen tödlichen Treffer abgefangen – jetzt ist sie weg."));
                break;
        }
    }

    public static void Reset()
    {
        LuckUsed.Clear();
        ArmorUsed.Clear();
    }
}
