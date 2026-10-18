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

    protected float Timer { get; set; }

    protected abstract string Label { get; }
    protected abstract KeyCode Key { get; }

    /// <summary>Volle Cooldown-Zeit (für die Anzeige und nach Benutzung/Meeting).</summary>
    protected abstract float Cooldown { get; }

    /// <summary>true = Cooldown ist der Kill-Cooldown des Spielers (Impostor-Fähigkeiten).</summary>
    protected virtual bool SharesKillTimer => false;

    /// <summary>Ob der Knopf überhaupt angezeigt wird (z.B. nur lebend).</summary>
    protected virtual bool Visible(PlayerControl local) => Players.IsAlive(local);

    /// <summary>Aktuelles Ziel; null = gerade nichts in Reichweite.</summary>
    protected abstract PlayerControl? FindTarget(PlayerControl local);

    /// <summary>Für Knöpfe ohne Spieler-Ziel (z.B. Leichen): ob gerade etwas in Reichweite ist.</summary>
    protected virtual bool HasTarget(PlayerControl local, PlayerControl? target) => target != null;

    protected abstract void Use(PlayerControl local, PlayerControl? target);

    /// <summary>Bild des Knopfs; null = Kill-Bild behalten.</summary>
    protected virtual Sprite? Icon(HudManager hud) => null;

    public void Reset()
    {
        if (_button != null) Object.Destroy(_button.gameObject);
        _button = null;
        Timer = Cooldown;
        _wasInMeeting = false;
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

        if (SharesKillTimer)
            Timer = local.killTimer;
        else if (local.CanMove && !local.inVent && Timer > 0f)
            Timer = Math.Max(0f, Timer - Time.deltaTime);

        var target = FindTarget(local);
        _button.SetTarget(target);
        var ready = Timer <= 0f && HasTarget(local, target) && local.CanMove;
        _button.SetCoolDown(Timer, Cooldown);
        if (ready) _button.SetEnabled();
        else _button.SetDisabled();

        if (ready && Input.GetKeyDown(Key)) Click(local);
    }

    private void Click(PlayerControl local)
    {
        if (Timer > 0f || !local.CanMove) return;
        var target = FindTarget(local);
        if (!HasTarget(local, target)) return;
        Use(local, target);
        if (!SharesKillTimer) Timer = Cooldown;
    }

    private void Create(HudManager hud)
    {
        var template = hud.KillButton;
        if (template == null) return;
        var button = Object.Instantiate(template, template.transform.parent);
        button.name = "SpecialRoles_" + GetType().Name;
        button.OverrideText(Label);
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
        SpecialRolesPlugin.Logger.LogInfo($"Knopf \"{Label}\" erstellt (Taste {Key}).");
    }
}
