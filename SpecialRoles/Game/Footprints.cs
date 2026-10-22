using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpecialRoles.Game;

/// <summary>
/// Merkt sich, wo alle Spieler in den letzten Sekunden waren (nur lokal, nur wenn eine Rolle es braucht:
/// Spurenleser, Medium) und zeichnet daraus Fußspuren. Positionen aller Spieler kennt jeder Client sowieso.
/// </summary>
internal static class Footprints
{
    private const float Interval = 0.25f;
    private const float Keep = 12f;
    private const float MinStep = 0.35f;

    private readonly struct Sample
    {
        public Sample(float time, Vector2 position)
        {
            Time = time;
            Position = position;
        }

        public float Time { get; }
        public Vector2 Position { get; }
    }

    private static readonly Dictionary<byte, List<Sample>> History = new();
    private static readonly List<(SpriteRenderer Renderer, float Until, float Alpha)> Shown = new();
    private static float _nextSample;

    /// <summary>Jeden Frame, solange die eigene Rolle Spuren braucht.</summary>
    public static void Record()
    {
        var now = Time.time;
        if (now < _nextSample) return;
        _nextSample = now + Interval;
        foreach (var player in Players.All())
        {
            if (!Players.IsAlive(player) || player.inVent) continue;
            if (!History.TryGetValue(player.PlayerId, out var list)) History[player.PlayerId] = list = new List<Sample>();
            var position = player.GetTruePosition();
            if (list.Count > 0 && Vector2.Distance(list[^1].Position, position) < MinStep) continue;
            list.Add(new Sample(now, position));
            while (list.Count > 0 && now - list[0].Time > Keep) list.RemoveAt(0);
        }
    }

    /// <summary>Zeigt die Spuren eines Spielers zwischen from und to (Time.time) für duration Sekunden.</summary>
    public static int Show(byte playerId, float from, float to, Color color, float duration) =>
        ShowPath(Snapshot(playerId, from, to), color, duration);

    /// <summary>Positionen eines Spielers zwischen from und to (älteste zuerst), z.B. um sie für später aufzuheben.</summary>
    public static List<Vector2> Snapshot(byte playerId, float from, float to)
    {
        var result = new List<Vector2>();
        if (!History.TryGetValue(playerId, out var list)) return result;
        foreach (var sample in list)
            if (sample.Time >= from && sample.Time <= to)
                result.Add(sample.Position);
        return result;
    }

    /// <summary>Zeichnet einen Weg (älteste Position zuerst); ältere Spuren blasser.</summary>
    public static int ShowPath(IReadOnlyList<Vector2> path, Color color, float duration)
    {
        for (var i = 0; i < path.Count; i++)
        {
            var age = path.Count <= 1 ? 0f : 1f - (float)i / (path.Count - 1);
            var alpha = Mathf.Lerp(0.85f, 0.25f, age);
            var renderer = Sprites.Spawn("Spur", Sprites.Circle, path[i], 0.18f, color with { a = alpha });
            Shown.Add((renderer, Time.time + duration, alpha));
        }
        return path.Count;
    }

    /// <summary>Jeden Frame: abgelaufene Spuren ausblenden.</summary>
    public static void Update()
    {
        var now = Time.time;
        for (var i = Shown.Count - 1; i >= 0; i--)
        {
            var (renderer, until, alpha) = Shown[i];
            if (renderer == null)
            {
                Shown.RemoveAt(i);
                continue;
            }
            var left = until - now;
            if (left <= 0f)
            {
                Object.Destroy(renderer.gameObject);
                Shown.RemoveAt(i);
            }
            else if (left < 1f)
            {
                renderer.color = renderer.color with { a = alpha * left };
            }
        }
    }

    public static void Reset()
    {
        foreach (var (renderer, _, _) in Shown)
            if (renderer != null) Object.Destroy(renderer.gameObject);
        Shown.Clear();
        History.Clear();
        _nextSample = 0f;
    }
}
