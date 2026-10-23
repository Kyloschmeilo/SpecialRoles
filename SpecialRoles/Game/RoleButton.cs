using System;
using SpecialRoles.Core;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpecialRoles.Game;

/// <summary>
/// Fähigkeits-Knopf einer Rolle: Klon des Kill-Knopfs (gleiches Aussehen, Cooldown-Anzeige, Ziel-Umriss) mit eigenem
/// Text, eigener Taste und eigenem Klick. Wird pro Runde neu gebaut, sobald die eigene Rolle bekannt ist.
/// </summary>
internal abstract class RoleButton
{
    private KillButton? _button;
    private bool _wasInMeeting;
    private string _shownLabel = "";
    private string _appliedText = "";
    private Sprite? _sprite;

    protected float Timer { get; set; }

    /// <summary>Text auf dem Knopf; darf sich ändern (z.B. „Tragen“ ↔ „Ablegen“, Einsätze).</summary>
    protected abstract string Label { get; }

    protected abstract KeyCode Key { get; }

    /// <summary>Volle Cooldown-Zeit (für die Anzeige und nach Benutzung/Meeting).</summary>
    protected abstract float Cooldown { get; }

    /// <summary>true = Cooldown ist der Kill-Cooldown des Spielers (Impostor-Fähigkeiten).</summary>
    protected virtual bool SharesKillTimer => false;

    /// <summary>Ob der Knopf überhaupt angezeigt wird (z.B. nur lebend).</summary>
    protected virtual bool Visible(PlayerControl local) => Players.IsAlive(local);

    /// <summary>Aktuelles Spieler-Ziel (bekommt den Umriss); null = keins.</summary>
    protected virtual PlayerControl? FindTarget(PlayerControl local) => null;

    /// <summary>Ob der Knopf gerade benutzbar ist (Standard: ein Spieler ist in Reichweite).</summary>
    protected virtual bool HasTarget(PlayerControl local, PlayerControl? target) => target != null;

    protected abstract void Use(PlayerControl local, PlayerControl? target);

    /// <summary>Bild des Knopfs; null = normales Kill-Bild.</summary>
    protected virtual Sprite? Icon(HudManager hud) => null;

    /// <summary>false = Use setzt den Timer selbst (z.B. erst nach Ende einer Wirkung).</summary>
    protected virtual bool CooldownAfterUse => true;

    /// <summary>false = auch benutzbar, wenn der Spieler gerade festgehalten wird (Kameramann beim Durchschauen).</summary>
    protected virtual bool NeedsMovement => true;

    public void Reset()
    {
        if (_button != null) Object.Destroy(_button.gameObject);
        _button = null;
        Timer = Cooldown;
        _wasInMeeting = false;
        _shownLabel = "";
        _appliedText = "";
        _sprite = null;
    }

    /// <summary>Cooldown von außen setzen (z.B. Schild gebrochen, Wirkung vorbei).</summary>
    public void SetTimer(float seconds) => Timer = seconds;

    /// <summary>Knopf verstecken, ohne ihn zu zerstören (z.B. Rolle gewechselt).</summary>
    public void Hide()
    {
        if (_button != null) _button.gameObject.SetActive(false);
    }

    public void Tick(HudManager hud, PlayerControl local)
    {
        if (_button == null) Create(hud);
        if (_button == null) return;

        var inMeeting = MeetingHud.Instance != null || ExileController.Instance != null;
        if (_wasInMeeting && !inMeeting) Timer = Cooldown; // nach jedem Meeting voller Cooldown, wie beim Kill
        _wasInMeeting = inMeeting;

        var visible = !inMeeting && Visible(local) && HudShown(hud);
        _button.gameObject.SetActive(visible);
        if (!visible) return;

        // Text und Bild jeden Frame prüfen: der Klon setzt beim Start die Kill-Beschriftung und das aktuelle Kill-Bild
        // (z.B. „ACID“ nach einer Viper-Runde) zurück
        var label = Label;
        var labelText = _button.buttonLabelText;
        if (label != _shownLabel || (labelText != null && labelText.text != _appliedText))
        {
            _button.OverrideText(ButtonText.Safe(label));
            _shownLabel = label;
            _appliedText = labelText != null ? labelText.text : "";
        }
        if (_sprite != null && _button.graphic != null && _button.graphic.sprite != _sprite)
            _button.graphic.sprite = _sprite;

        if (SharesKillTimer)
            Timer = local.killTimer;
        else if (local.CanMove && !local.inVent && Timer > 0f)
            Timer = Math.Max(0f, Timer - Time.deltaTime);

        var target = FindTarget(local);
        _button.SetTarget(target);
        var ready = Timer <= 0f && HasTarget(local, target) && CanAct(local);
        _button.SetCoolDown(Timer, Math.Max(Cooldown, 0.01f));
        if (ready) _button.SetEnabled();
        else _button.SetDisabled();

        if (ready && Input.GetKeyDown(Key)) Click(local);
    }

    /// <summary>
    /// Ob die Spiel-Knöpfe gerade zu sehen sind. Unten rechts steht je nach Lage der Benutzen- oder der Haustier-Knopf
    /// (ohne etwas Benutzbares in der Nähe), darum reicht einer von beiden.
    /// </summary>
    private static bool HudShown(HudManager hud) =>
        (hud.UseButton != null && hud.UseButton.isActiveAndEnabled) ||
        (hud.PetButton != null && hud.PetButton.isActiveAndEnabled);

    private bool CanAct(PlayerControl local) =>
        local.CanMove || (!NeedsMovement && Minigame.Instance == null && !Players.InMeeting);

    private void Click(PlayerControl local)
    {
        if (Timer > 0f || !CanAct(local) || !Visible(local)) return;
        var target = FindTarget(local);
        if (!HasTarget(local, target)) return;
        Use(local, target);
        if (!SharesKillTimer && CooldownAfterUse) Timer = Cooldown;
    }

    private void Create(HudManager hud)
    {
        var template = hud.KillButton;
        if (template == null) return;
        var button = Object.Instantiate(template, template.transform.parent);
        button.name = "SpecialRoles_" + GetType().Name;
        // Übersetzer abschalten, sonst schreibt er „KILL“ zurück
        foreach (var translator in button.GetComponentsInChildren<TextTranslatorTMP>(true))
            translator.enabled = false;
        // ohne eigenes Bild das normale Kill-Bild, nicht das gerade eingestellte einer Spielrolle
        var icon = Icon(hud);
        if (icon == null) icon = template.defaultKillSprite;
        if (icon == null) icon = button.graphic.sprite;
        _sprite = icon;
        if (icon != null) button.graphic.sprite = icon;

        var passive = button.GetComponent<PassiveButton>();
        if (passive != null)
        {
            passive.OnClick = new Button.ButtonClickedEvent();
            passive.OnClick.AddListener((Action)(() =>
            {
                var local = PlayerControl.LocalPlayer;
                if (local != null) Safe.Run(() => Click(local));
            }));
        }
        _button = button;
        _shownLabel = "";
        _appliedText = "";
        SpecialRolesPlugin.Logger.LogInfo($"Knopf \"{Label}\" erstellt (Taste {Key}).");
    }
}
