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
  `RoleAssigner.cs` (Vergabe), `Protocol.cs` (RPC-IDs 210–212, `RoleSync` als Bytes, versioniert).
- `Game/` – Spiel-Anbindung: `Patches.cs` (alle Harmony-Patches, Parameter positional `__0`), `Distribution.cs`
  (Host verteilt nach `RoleManager.SelectRoles`), `Net.cs` (eigene RPCs über `PlayerControl.HandleRpc`; `SetRoles` nur
  vom Host akzeptiert), `RoleState.cs`, `Hud.cs` (Namensschild, Aufgabenliste, Knöpfe je Rolle),
  `RoleButton.cs` (Klon des Kill-Knopfs: `SetTarget`, `SetCoolDown`, eigener Klick + Taste; nicht `AbilityButton`
  nennen – die Klasse gibt es im Spiel schon, sie gewinnt bei der Namensauflösung).
- `Roles/` – Verhalten je Rolle. Neue Rolle: Klasse hier, in `RoleCatalog` `Implemented = true`, ggf. Knopf in
  `Hud.Buttons`, Einstellungen über `RoundSettings` (gehen mit `SetRoles` an alle → `RoleSync.Version` erhöhen).
- Kills laufen immer über den Host (`RpcMurderPlayer`), Clients schicken nur Wünsche.

## Bauen & Testen
- Cloud: `nuget.bepinex.dev` ist von dort gesperrt → Mod nur auf GitHub Actions bauen (`build.yml`, bei jedem Push),
  Fehler über die Job-Logs lesen. `dotnet test Tests` geht lokal (nur nuget.org). `nuget.config` liegt deshalb in
  `SpecialRoles/`, nicht im Wurzelordner.
- Windows: wie CrewStats; **nur ins Spiel kopieren, wenn Kilian es sagt** → `-p:AmongUs=C:/nicht-kopieren`.
- Release: `release.yml` (workflow_dispatch, Version aus der csproj, Notizen `release-notes/vX.Y.Z.md`, 0.x = Pre-Release).
  **Nur auf Kilians Wunsch** auslösen.
- Mehrspieler lässt sich allein nicht testen (Spiel startet nur einmal) → jeder Schritt braucht einen Testabend.

## Stand / offen
- 0.1.0 (17.10.2026, Pre-Release auf GitHub seit 18.10.): Grundgerüst + Sheriff, Putzkraft, Nachtsicht, Flash.
  **Noch nie im Spiel gelaufen** – nächster Schritt ist ein Testabend mit 2–3 Leuten.
- Unbekannt: ob offizielle Server eigene RPC-IDs oder Host-Kills durch Crewmates (Sheriff) als „Hacking“ werten.
- Fehlt noch: Lobby-Einstellungen im Spiel (bisher Config des Hosts), Rolle im Intro, Versions-Check in der Lobby,
  Installer/Auto-Update (Vorlage: CrewStats `Updater.cs` + Installer), Siegbedingung Narr.
