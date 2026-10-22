using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpecialRoles.Game;

/// <summary>
/// Pfeile rund um den eigenen Spieler, die auf andere Spieler zeigen (Snitch). Nur lokal sichtbar.
/// Jede Rolle setzt jeden Frame die Ziele, die sie gerade braucht; was nicht mehr gesetzt wird, verschwindet.
/// </summary>
internal static class Arrows
{
    private const float Distance = 0.9f;
    private const float Scale = 0.35f;

    private static readonly Dictionary<string, SpriteRenderer> Shown = new();
    private static readonly HashSet<string> Wanted = new();

    /// <summary>Pfeil mit Schlüssel key (z.B. „snitch:3“) zeigt diesen Frame auf target.</summary>
    public static void Point(string key, PlayerControl local, Vector2 target, Color color)
    {
        Wanted.Add(key);
        if (!Shown.TryGetValue(key, out var arrow) || arrow == null)
            Shown[key] = arrow = Sprites.Spawn("Pfeil", Sprites.Arrow, local.GetTruePosition(), Scale, color, -1f);

        var from = (Vector2)local.transform.position;
        var direction = target - local.GetTruePosition();
        var visible = direction.magnitude > Distance + 0.3f;
        arrow.enabled = visible;
        if (!visible) return;
        direction.Normalize();
        var position = from + direction * Distance;
        arrow.transform.position = new Vector3(position.x, position.y, -1f);
        arrow.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        arrow.color = color;
    }

    /// <summary>Am Ende jedes Frames: Pfeile entfernen, die diesen Frame niemand gesetzt hat.</summary>
    public static void EndFrame()
    {
        if (Shown.Count > 0)
        {
            var remove = new List<string>();
            foreach (var (key, arrow) in Shown)
                if (!Wanted.Contains(key) || arrow == null)
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
        foreach (var arrow in Shown.Values)
            if (arrow != null) Object.Destroy(arrow.gameObject);
        Shown.Clear();
        Wanted.Clear();
    }
}
