using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SpecialRoles.Game;

/// <summary>
/// Hinweise oben in der Bildschirmmitte, nur für einen selbst (kein Chat – Quick-Chat-Spieler würden sonst gekickt).
/// Text-Objekt = Klon der Beschriftung des Kill-Knopfs (gleiche Schrift mit Umriss).
/// </summary>
internal static class Notify
{
    private const float Duration = 5f;
    private const int MaxLines = 3;

    private static readonly List<(string Text, float Until)> Lines = new();
    private static TextMeshPro? _text;

    public static void Show(string text, float duration = Duration)
    {
        Lines.Add((text, Time.time + duration));
        while (Lines.Count > MaxLines) Lines.RemoveAt(0);
        SpecialRolesPlugin.Logger.LogInfo($"Hinweis: {text}");
        Refresh();
    }

    /// <summary>Jeden Frame: abgelaufene Zeilen entfernen.</summary>
    public static void Update()
    {
        if (Lines.Count == 0) return;
        var removed = Lines.RemoveAll(l => Time.time >= l.Until);
        if (removed > 0 || _text == null) Refresh();
    }

    private static void Refresh()
    {
        var text = Text();
        if (text == null) return;
        text.text = string.Join("\n", Lines.ConvertAll(l => l.Text));
        text.gameObject.SetActive(Lines.Count > 0);
    }

    private static TextMeshPro? Text()
    {
        if (_text != null) return _text;
        var hud = HudManager.Instance;
        var template = hud != null && hud.KillButton != null ? hud.KillButton.buttonLabelText : null;
        if (template == null) return null;

        var text = Object.Instantiate(template, hud!.transform);
        text.name = "SpecialRoles_Hinweis";
        var translator = text.GetComponent<TextTranslatorTMP>();
        if (translator != null) Object.Destroy(translator);
        text.transform.localPosition = new Vector3(0f, 1.9f, -50f);
        text.transform.localScale = Vector3.one;
        text.enableAutoSizing = false;
        text.fontSize = 2.6f;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.rectTransform.sizeDelta = new Vector2(10f, 2f);
        _text = text;
        return text;
    }

    public static void Reset()
    {
        Lines.Clear();
        if (_text != null) Object.Destroy(_text.gameObject);
        _text = null;
    }
}
