# SpecialRoles

Among-Us-Mod (C#, BepInEx 6 IL2CPP x64, wie CrewStats) mit 30 neuen Rollen. Gehört zum Projekt „Spiele-Netzwerk“
(Discord-Bot Kyloschmeilo/AmongUsManager, Mod Kyloschmeilo/CrewStats). Kommunikation mit dem Nutzer (Kilian): **Deutsch**.

## Entscheidungen (Kilian, 17.10.2026)
- **Unabhängig von CrewStats**: keine Rollenwünsche, kein Webhook. Beide Mods können gleichzeitig laufen
  (`SelectRoles`-Postfix hier mit `Priority.Last`, also nach CrewStats' Würfeln/Wünschen).
- Rollenliste (30): Crew Sheriff, Medic, Bürgermeister, Snitch, Hacker, Spurenleser, Fallensteller, Medium, Veteran,
  Nachtsicht, Altruist, Mechaniker, Transporter, Orakel, Kameramann, Türwächter, Glückspilz; Impostor Putzkraft,
  Bestatter, Vampir, Bomber, Grenadier, Maulwurf, Erpresser (**max. 2× pro Spiel**), Tarner, Hexenmeister, Armor,
  Saboteur; Neutral Narr, Vergesslicher. Modifikator **Flash** (statt „Verliebte“). Texte im Bot:
  `content/SpecialRoles.kt` in AmongUsManager (`/special-rolle`) – Namen/Beschreibungen gemeinsam ändern.
- Fair bleiben: Vergabe mit `RandomNumberGenerator`, Host ohne Vorteil, jeder sieht nur seine eigene Rolle.
  Keine Chat-Nachrichten ins Spiel (Quick-Chat-Spieler werden sonst gekickt, siehe CrewStats-Historie).

## Aufbau (`SpecialRoles/`)
- `Core/` – spielunabhängig, auch von `Tests/` kompiliert: `Roles.cs` (`RoleId` – Werte gehen übers Netz, nie
  ändern; `RoleCatalog` mit `Implemented`-Flag: nur fertige Rollen werden vergeben/haben Config),
  `RoundSettings.cs` (`SettingId` + `SettingCatalog`: alle Rollen-Einstellungen mit Standard/Min/Max, Config-Abschnitt
  = Rollenname), `RoleAssigner.cs` (Vergabe), `Protocol.cs` (RPC-IDs 210–233, `RoleSync` v2 = Rollen + Einstellungen
  als Liste (Id, Wert), unbekannte Ids werden ignoriert), `Png.cs` (Bilder als PNG für `ImageConversion.LoadImage`).
- `Game/` – Spiel-Anbindung: `Patches.cs` (alle Harmony-Patches, Parameter positional `__0`), `Round.cs` (läuft jeden
  Frame aus HudManager.Update: Meeting-Beginn/-Ende, alle Rollen-Updates, `EndGame`/`NewGame`), `Net.cs` (eigene RPCs
  über `PlayerControl.HandleRpc`, Host-Nachrichten werden nur vom Host angenommen), `Attack.cs` (Host: **jeder**
  Angriff läuft über `Contact` (wachsamer Veteran) und `Lethal` (Medic-Schild, Glückspilz, Armor); `Kill` = Selbst-Kill
  ohne Teleport), `Deaths.cs` (wer wann wo starb, Weg des Mörders fürs Medium, Spielrolle vor dem Tod),
  `Distribution.cs`, `RoleState.cs`, `Hud.cs` (Knöpfe je Rolle, Name, Meeting-Zusätze), `RoleButton.cs` (Klon des
  Kill-Knopfs; nicht `AbilityButton` nennen – die Klasse gibt es im Spiel schon), Bausteine `Notify` (Hinweis oben
  mittig, Klon der Kill-Knopf-Schrift), `Arrows`, `Markers` (Ringe), `Footprints`, `Bodies`, `Speed` (Flash ×
  Bestatter), `Sprites` (Kreis/Ring/Pfeil/Quadrat im Code gezeichnet), `Players`, `Colors`.
- `Roles/` – Verhalten je Rolle (statische Klasse, Knopf als `Button`). Neue Rolle: Klasse hier, in `RoleCatalog`
  `Implemented = true`, Knopf in `Hud.Buttons`, Update/Reset in `Round`, Einstellungen in `SettingCatalog`.
- Kills, Wiederbeleben, Türen, Reparieren, Rollenwechsel laufen über den Host; Clients schicken nur Wünsche.
  Bewegen darf sich jeder nur selbst (Transporter/Altruist: der Betroffene macht `RpcSnapTo` auf seinem Client).
- Bewusste Vereinfachungen: Narr gewinnt mit eigenem Spielende-Grund 70 (CrewStats: „unbekannt“), seine Tasks zählen
  normal; Bombe/Fluch/Biss töten per Selbst-Kill (Medium sieht beim Biss den Vampir, sonst „keine Spur“).

## Bauen & Testen
- Cloud: `nuget.bepinex.dev` ist von dort gesperrt → Mod nur auf GitHub Actions bauen (`build.yml`, bei jedem Push),
  Fehler über die Job-Logs lesen. Spiel-Signaturen nachschlagen: Workflow **API-Dump** (`api-dump.yml`, Eingabe z.B.
  `PlayerControl.Rpc* MeetingHud`) → Ausgabe im Job-Log (`tools/ApiDump`, MetadataLoadContext gegen die Interop-DLLs).
  Unity-Funktionen, die das Spiel nie benutzt, können trotz Interop zur Laufzeit fehlen → bewährte Wege nehmen
  (Bilder per `ImageConversion.LoadImage`, kein `ColorUtility`/`SetPixels`). `dotnet test Tests` geht lokal (nur nuget.org). `nuget.config` liegt deshalb in
  `SpecialRoles/`, nicht im Wurzelordner.
- Windows: wie CrewStats; **nur ins Spiel kopieren, wenn Kilian es sagt** → `-p:AmongUs=C:/nicht-kopieren`.
- Release: `release.yml` (workflow_dispatch, Version aus der csproj, Notizen `release-notes/vX.Y.Z.md`, 0.x = Pre-Release,
  hängt DLL + Installer an; gibt es das Release schon, ersetzt er nur den Installer). **Nur auf Kilians Wunsch** auslösen.
- Installer `installer/SpecialRoles-Installer.bat` = CrewStats-Installer ohne Webhook-Schritt (Batch/PowerShell-Hybrid,
  **CRLF, ohne BOM**). Holt die neueste DLL aus `releases` **inkl. Pre-Releases** (`releases/latest` überspringt die),
  Deinstallieren lässt BepInEx an, wenn andere Mods (CrewStats) da sind. Testen mit Fake-Ordner:
  `SPECIALROLES_GAME_DIR`, `SPECIALROLES_BEPINEX_ZIP`, `SPECIALROLES_MOD_DLL` (geht auch in der Cloud mit portablem pwsh
  von github.com/PowerShell/PowerShell/releases, `Read-Host` per Funktion überschreiben).
- Mehrspieler lässt sich allein nicht testen (Spiel startet nur einmal) → jeder Schritt braucht einen Testabend.

## Stand / offen
- 0.1.0 (17.10.2026, Pre-Release auf GitHub seit 18.10.): Grundgerüst + Sheriff, Putzkraft, Nachtsicht, Flash.
  Von Kilian getestet: „alles klappt“ (22.10.2026).
- 0.2.0 (22.10.2026, Branch `main`, noch **nicht** veröffentlicht): alle 30 Rollen. Baut auf GitHub, **noch nie im
  Spiel gelaufen** – nächster Schritt ist ein Testabend (Checkliste je Rolle; TestLab-Bots helfen für Einzeltests).
- Unbekannt: ob offizielle Server eigene RPC-IDs, Host-Kills durch Crewmates (Sheriff), eigene Vent-Ids (Maulwurf)
  oder `RpcSetRole` mitten im Spiel (Vergesslicher, Altruist) als „Hacking“ werten.
- Fehlt noch: Lobby-Einstellungen im Spiel (bisher Config des Hosts), Rolle im Intro, Versions-Check in der Lobby,
  Auto-Update (Vorlage: CrewStats `Updater.cs`; muss Pre-Releases berücksichtigen).
