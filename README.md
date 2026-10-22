# SpecialRoles – 30 neue Rollen für Among Us

Eine BepInEx-Mod für **Among Us auf Steam (64-Bit)** mit 30 neuen Rollen: 17 für die Crew, 11 für Impostor und
2 neutrale, dazu den Modifikator **Flash**. Alle Rollen erklärt der Discord-Bot mit `/special-rolle`.

> **Stand 0.2.1:** alle 30 Rollen sind eingebaut und lokal getestet, aber noch nicht im echten Spiel mit mehreren Leuten.

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
- Fähigkeiten haben einen eigenen Knopf neben dem Kill-Knopf, Taste `F` (Sheriff `Q`, Kameramann zusätzlich `G`).
- Hinweise der Rollen erscheinen oben in der Bildschirmmitte, nur für einen selbst – die Mod schreibt nie in den Chat.

## Rollen

| Rolle | Team | Taste | Was sie kann |
|-------|------|-------|--------------|
| Sheriff | Crew | Q | Schießt auf einen Spieler in Reichweite. Impostor → der stirbt, sonst stirbt der Sheriff. |
| Medic | Crew | F | Gibt einem Spieler einen Schild gegen den nächsten tödlichen Angriff. Bricht er, erfahren es Medic und Geschützter. |
| Bürgermeister | Crew | – | Seine Stimme zählt doppelt (zweites Stimm-Symbol). |
| Snitch | Crew | – | Alle Tasks fertig → Pfeile zu den Impostorn. Fehlt ihm nur noch ein Task, sehen die Impostor einen Pfeil zu ihm. |
| Hacker | Crew | F | Admin-Karte von überall (man steht dabei still, wie am Admin-Tisch). |
| Spurenleser | Crew | F | Fußspuren aller Spieler der letzten 10 Sekunden, in ihren Farben. |
| Fallensteller | Crew | F | Fallen, die nur er sieht. Läuft jemand hinein, erfährt er den Namen. |
| Medium | Crew | F | Zeigt an einer Leiche (höchstens 60 s alt) grau den Weg des Mörders kurz vor der Tat. |
| Veteran | Crew | F | Wird wachsam: wer ihn dann angreift (Kill, Schuss, Biss, Bombe …), stirbt selbst. |
| Nachtsicht | Crew | – | Sieht bei Licht-Sabotage ganz normal weiter. |
| Altruist | Crew | F | Belebt eine Leiche wieder und stirbt dabei selbst. |
| Mechaniker | Crew | F | Repariert einmal pro Runde alle Sabotagen von überall. |
| Transporter | Crew | F | Markiert einen Spieler und tauscht ihn dann mit einem zweiten. |
| Orakel | Crew | F | Wählt einen Spieler; stirbt das Orakel, erfahren alle dessen Team (auch im Meeting am Namen). |
| Kameramann | Crew | F / G | Stellt zwei eigene Kameras auf (F) und schaut durch sie hindurch (G). |
| Türwächter | Crew | F | Schließt die Türen aller Räume in seiner Nähe. |
| Glückspilz | Crew | – | Überlebt einmal pro Spiel einen Angriff (Chance einstellbar, Standard 50 %). |
| Putzkraft | Impostor | F | Lässt eine Leiche verschwinden; teilt sich den Cooldown mit dem Kill. |
| Bestatter | Impostor | F | Trägt eine Leiche (langsamer) und legt sie woanders ab. |
| Vampir | Impostor | Kill | Beißt statt zu töten: das Opfer stirbt 10 s später an Ort und Stelle, spätestens vor dem nächsten Meeting. |
| Bomber | Impostor | F | Klebt eine Bombe an; nach 10 s sterben der Träger und alle Crewmates im Radius. Ein Meeting entschärft sie. |
| Grenadier | Impostor | F | Blendgranate: Crewmates in der Nähe sehen ein paar Sekunden fast nur Weiß. |
| Maulwurf | Impostor | F | Baut bis zu 3 eigene Vents, die miteinander verbunden sind. |
| Erpresser | Impostor | F | Ein Spieler darf im nächsten Meeting nicht schreiben und nicht reden („erpresst“ am Namen). 2× pro Spiel. |
| Tarner | Impostor | F | Alle sehen ein paar Sekunden gleich grau aus, ohne Namen. |
| Hexenmeister | Impostor | F | Verflucht einen Spieler und löst später aus: der Nächste neben dem Verfluchten stirbt. |
| Armor | Impostor | – | Überlebt den ersten tödlichen Treffer (Sheriff, Bombe, Veteran …). |
| Saboteur | Impostor | – | Solange er lebt, ist der Sabotage-Cooldown halb so lang. |
| Narr | Neutral | – | Wird er rausgewählt, gewinnt er allein. |
| Vergesslicher | Neutral | F | Übernimmt an einer Leiche deren Rolle (war der Tote Impostor, wird er Impostor). |
| Flash | Modifikator | – | Läuft schneller als alle anderen. Kann jeder zusätzlich zu seiner Rolle bekommen. |

## Einstellungen

`BepInEx/config/de.spielenetzwerk.specialroles.cfg` (nur bei geschlossenem Spiel ändern). Es zählen die
Einstellungen des **Hosts**: pro Rolle *Anzahl* und *Chance* (Standard zum Testen: je 1 × 100 % – dann bekommt fast
jeder eine Rolle; auf 0 stellen schaltet eine Rolle ab), dazu je Rolle Cooldowns, Dauer, Einsätze usw. und
Flash-Anzahl/-Chance/-Tempo.

## Bauen

```
dotnet build SpecialRoles -c Release                                  # kopiert nach BepInEx/plugins
dotnet build SpecialRoles -c Release -p:AmongUs=C:/nicht-kopieren     # nur bauen
dotnet test Tests                                                     # Tests der Rollen-Logik
```

Jeder Push baut die DLL auch auf GitHub (Actions → Build → Artifacts).
