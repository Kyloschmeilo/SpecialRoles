using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpecialRoles.Game;

/// <summary>
/// Ringe unter Spielern (Medic-Schild, Bombe), nur lokal sichtbar. Wie <see cref="Arrows"/>: jede Rolle setzt ihre
/// Ringe jeden Frame, nicht mehr gesetzte verschwinden.
/// </summary>
internal static class Markers
{
    private static readonly Dictionary<string, SpriteRenderer> Shown = new();
    private static readonly HashSet<string> Wanted = new();

    public static void Ring(string key, PlayerControl player, Color color, float scale = 0.9f)
    {
        Wanted.Add(key);
        if (!Shown.TryGetValue(key, out var ring) || ring == null)
            Shown[key] = ring = Sprites.Spawn("Ring", Sprites.Ring, player.GetTruePosition(), scale, color);

        var position = player.GetTruePosition();
        // knapp hinter dem Spieler, damit er nicht verdeckt wird
        ring.transform.position = new Vector3(position.x, position.y, player.transform.position.z + 0.001f);
        ring.color = color;
        ring.enabled = player.Visible && !player.inVent;
    }

    public static void EndFrame()
    {
        if (Shown.Count > 0)
        {
            var remove = new List<string>();
            foreach (var (key, ring) in Shown)
                if (!Wanted.Contains(key) || ring == null)
                    remove.Add(key);
            foreach (var key in remove)
            {
                if (Shown[key] != null) Object.Destroy(Shown[key].gameObject);
                Shown.Remove(key);
            }
        }
        Wanted.Clear();
    }

    public static void Reset()
    {
        foreach (var ring in Shown.Values)
            if (ring != null) Object.Destroy(ring.gameObject);
        Shown.Clear();
        Wanted.Clear();
    }
}
