using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpecialRoles.Game;

/// <summary>Leichen auf der Karte (DeadBody.ParentId = PlayerId des Toten).</summary>
internal static class Bodies
{
    /// <summary>Reichweite für Fähigkeiten an Leichen (wie Melden).</summary>
    public const float Range = 1.8f;

    public static IEnumerable<DeadBody> All()
    {
        foreach (var body in Object.FindObjectsOfType<DeadBody>())
            if (body != null && !body.Reported)
                yield return body;
    }

    public static DeadBody? ById(byte playerId)
    {
        foreach (var body in All())
            if (body.ParentId == playerId)
                return body;
        return null;
    }

    public static DeadBody? Closest(Vector2 position, float range = Range, System.Func<DeadBody, bool>? filter = null)
    {
        DeadBody? best = null;
        var bestDistance = range;
        foreach (var body in All())
        {
            if (filter != null && !filter(body)) continue;
            var distance = Vector2.Distance(position, body.TruePosition);
            if (distance <= bestDistance)
            {
                best = body;
                bestDistance = distance;
            }
        }
        return best;
    }

    public static void Remove(byte playerId)
    {
        foreach (var body in Object.FindObjectsOfType<DeadBody>())
            if (body != null && body.ParentId == playerId)
                Object.Destroy(body.gameObject);
    }

    /// <summary>Leiche verschieben (TruePosition = Position + Collider-Versatz, daher über die Differenz).</summary>
    public static void MoveTo(DeadBody body, Vector2 truePosition)
    {
        var offset = truePosition - body.TruePosition;
        var position = body.transform.position;
        var x = position.x + offset.x;
        var y = position.y + offset.y;
        body.transform.position = new Vector3(x, y, y / 1000f + 0.0005f); // knapp hinter Spielern auf gleicher Höhe
    }
}
