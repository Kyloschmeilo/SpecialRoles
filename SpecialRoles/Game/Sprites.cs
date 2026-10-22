using System;
using UnityEngine;

namespace SpecialRoles.Game;

/// <summary>
/// Einfache Bilder, im Code gezeichnet (keine Dateien nötig): Kreis, Ring, Pfeil. Weiß – eingefärbt wird über
/// SpriteRenderer.color. 1 Bild = 1 Spieleinheit groß.
/// </summary>
internal static class Sprites
{
    private const int Size = 64;

    private static Sprite? _circle;
    private static Sprite? _ring;
    private static Sprite? _arrow;
    private static Sprite? _square;

    public static Sprite Circle => _circle ??= Draw((x, y) => Edge(1f - Radius(x, y)));

    public static Sprite Ring => _ring ??= Draw((x, y) =>
    {
        var r = Radius(x, y);
        return Math.Min(Edge(1f - r), Edge(r - 0.75f));
    });

    /// <summary>Dreieck, das nach rechts zeigt (für ArrowBehaviour/Rotation).</summary>
    public static Sprite Arrow => _arrow ??= Draw((x, y) =>
    {
        // Spitze rechts (x = 1), Basis links (x = -0.6), Höhe bis |y| = 0.8
        if (x < -0.6f || x > 1f) return 0f;
        var halfHeight = 0.8f * (1f - x) / 1.6f;
        return Edge((halfHeight - Math.Abs(y)) * 8f);
    });

    public static Sprite Square => _square ??= Draw((_, _) => 1f);

    /// <summary>Weißes Objekt mit Bild in der Welt (z = etwas vor dem Boden, hinter Spielern).</summary>
    public static SpriteRenderer Spawn(string name, Sprite sprite, Vector2 position, float scale, Color color, float z = 0.01f)
    {
        var obj = new GameObject("SpecialRoles_" + name);
        obj.transform.position = new Vector3(position.x, position.y, position.y / 1000f + z);
        obj.transform.localScale = new Vector3(scale, scale, 1f);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        return renderer;
    }

    private static float Radius(float x, float y) => (float)Math.Sqrt(x * x + y * y);

    /// <summary>Weiche Kante: Wert > 0 = innen.</summary>
    private static float Edge(float inside) => Math.Clamp(inside * Size / 4f, 0f, 1f);

    /// <param name="alpha">x/y von -1 bis 1 → Deckkraft 0–1.</param>
    private static Sprite Draw(Func<float, float, float> alpha)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave,
        };
        var pixels = new Color32[Size * Size];
        for (var py = 0; py < Size; py++)
        for (var px = 0; px < Size; px++)
        {
            var x = (px + 0.5f) / Size * 2f - 1f;
            var y = (py + 0.5f) / Size * 2f - 1f;
            pixels[py * Size + px] = new Color32(255, 255, 255, (byte)(255 * Math.Clamp(alpha(x, y), 0f, 1f)));
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
