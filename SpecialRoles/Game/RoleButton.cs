using System;
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

    /// <summary>Bild des Knopfs; null = Kill-Bild behalten.</summary>
    protected virtual Sprite? Icon(HudManager hud) => null;

    /// <summary>false = Use setzt den Timer selbst (z.B. erst nach Ende einer Wirkung).</summary>
    protected virtual bool CooldownAfterUse => true;

    public void Reset()
    {
        if (_button != null) Object.Destroy(_button.gameObject);
        _button = null;
        Timer = Cooldown;
        _wasInMeeting = false;
        _shownLabel = "";
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

        var visible = !inMeeting && Visible(local) && hud.UseButton != null && hud.UseButton.isActiveAndEnabled;
        _button.gameObject.SetActive(visible);
        if (!visible) return;

        var label = Label;
        if (label != _shownLabel)
        {
            _button.OverrideText(label);
            _shownLabel = label;
        }

        if (SharesKillTimer)
            Timer = local.killTimer;
        else if (local.CanMove && !local.inVent && Timer > 0f)
            Timer = Math.Max(0f, Timer - Time.deltaTime);

        var target = FindTarget(local);
        _button.SetTarget(target);
        var ready = Timer <= 0f && HasTarget(local, target) && local.CanMove;
        _button.SetCoolDown(Timer, Math.Max(Cooldown, 0.01f));
        if (ready) _button.SetEnabled();
        else _button.SetDisabled();

        if (ready && Input.GetKeyDown(Key)) Click(local);
    }

    private void Click(PlayerControl local)
    {
        if (Timer > 0f || !local.CanMove || !Visible(local)) return;
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
        var icon = Icon(hud);
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
        SpecialRolesPlugin.Logger.LogInfo($"Knopf \"{Label}\" erstellt (Taste {Key}).");
    }
}
