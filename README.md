# SpecialRoles – 30 neue Rollen für Among Us

Eine BepInEx-Mod für **Among Us auf Steam (64-Bit)** mit 30 neuen Rollen: 17 für die Crew, 11 für Impostor und
2 neutrale, dazu den Modifikator **Flash**. Alle Rollen erklärt der Discord-Bot mit `/special-rolle`.

> **Stand 0.1.0:** Grundgerüst zum Testen. Fertig sind **Sheriff**, **Putzkraft**, **Nachtsicht** und **Flash**,
> die übrigen Rollen kommen nach und nach.

## Installieren

1. Unter [**Releases**](../../releases) beim neuesten Release die Datei **`SpecialRoles-Installer.bat`** herunterladen.
2. Doppelklicken. Windows warnt evtl. vor einer unbekannten Datei → **Weitere Informationen → Trotzdem ausführen**.
3. Enter drücken – der Installer findet Among Us, installiert BepInEx (64-Bit, falls noch nicht da) und die Mod.

Neue Version: Installer einfach nochmal starten. Deinstallieren: Installer starten und **2** wählen
(CrewStats und andere Mods bleiben erhalten). Der erste Spielstart dauert 1–2 Minuten.

## Wie es funktioniert

- **Alle Spieler der Lobby brauchen die Mod in derselben Version.**
- Der **Host** verteilt beim Rundenstart die Mod-Rollen fair mit echtem Zufall – zusätzlich zu den Spielrollen.
  Crewmates bekommen Crew- oder Neutral-Rollen, Impostor nur Impostor-Rollen. Wer im Spiel schon eine Spezialrolle
  hat (Engineer, Shapeshifter …), bekommt keine Mod-Rolle. Flash kann jeder zusätzlich bekommen.
- Jeder sieht nur seine eigene Rolle: unter dem eigenen Namen und oben in der Aufgabenliste.
- Fähigkeiten haben einen eigenen Knopf neben dem Kill-Knopf, mit Taste (Sheriff `Q`, Putzkraft `F`).

## Fertige Rollen

| Rolle | Team | Was sie kann |
|-------|------|--------------|
| Sheriff | Crew | Schießt auf einen Spieler in Reichweite. Impostor → der stirbt, sonst stirbt der Sheriff. |
| Nachtsicht | Crew | Sieht bei Licht-Sabotage ganz normal weiter. |
| Putzkraft | Impostor | Lässt eine Leiche verschwinden; teilt sich den Cooldown mit dem Kill. |
| Flash | Modifikator | Läuft schneller als alle anderen. |

## Einstellungen

`BepInEx/config/de.spielenetzwerk.specialroles.cfg` (nur bei geschlossenem Spiel ändern). Es zählen die
Einstellungen des **Hosts**: pro Rolle *Anzahl* und *Chance* (Standard zum Testen: je 1 × 100 %), Sheriff-Cooldown,
Flash-Anzahl/-Chance/-Tempo.

## Bauen

```
dotnet build SpecialRoles -c Release                                  # kopiert nach BepInEx/plugins
dotnet build SpecialRoles -c Release -p:AmongUs=C:/nicht-kopieren     # nur bauen
dotnet test Tests                                                     # Tests der Rollen-Logik
```

Jeder Push baut die DLL auch auf GitHub (Actions → Build → Artifacts).
