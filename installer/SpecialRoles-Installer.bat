<# : SpecialRoles-Installer - der Batch-Teil startet nur PowerShell mit dem Rest dieser Datei
@echo off
chcp 65001 >nul
powershell -NoProfile -ExecutionPolicy Bypass -Command "Invoke-Expression (Get-Content -LiteralPath '%~f0' -Raw -Encoding UTF8)"
echo.
pause
exit /b
#>

# ==========================================================================================
#  SpecialRoles-Installer: installiert BepInEx (64-Bit) + die Rollen-Mod SpecialRoles in Among Us (Steam)
#  und aktualisiert sie. Einfach nochmal starten = reparieren/aktualisieren.
#  Quelle: https://github.com/Kyloschmeilo/SpecialRoles
#  Vorlage: CrewStats-Installer (gleiches BepInEx, beide Mods können zusammen installiert sein)
# ==========================================================================================

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'   # sonst sind Downloads in PowerShell 5 extrem langsam
[Console]::OutputEncoding = [Text.Encoding]::UTF8
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$Host.UI.RawUI.WindowTitle = 'SpecialRoles Installer'

$Repo          = 'Kyloschmeilo/SpecialRoles'   # GitHub-Repo mit den Releases
$BepInExUrl    = 'https://builds.bepinex.dev/projects/bepinex_be/735/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.735%2B5fef357.zip'
$BepInExSha256 = 'BADEF8112853A00939A0DF6CA143BC0A4E3DC02BD4D21B873302731BFA0E4DF4'
$ModDll        = 'SpecialRoles.dll'

function Step($text) { Write-Host "`n> $text" -ForegroundColor Cyan }
function Ok($text)   { Write-Host "  OK  $text" -ForegroundColor Green }
function Warn($text) { Write-Host "  !!  $text" -ForegroundColor Yellow }
function Fail($text) { Write-Host "`n  FEHLER: $text" -ForegroundColor Red; exit 1 }

function Get-Machine([string]$path) {
    # Liest aus dem PE-Header, ob eine .exe/.dll 32- oder 64-Bit ist
    $stream = [IO.File]::OpenRead($path)
    try {
        $buffer = New-Object byte[] 4096
        [void]$stream.Read($buffer, 0, $buffer.Length)
        $pe = [BitConverter]::ToInt32($buffer, 0x3C)
        return [BitConverter]::ToUInt16($buffer, $pe + 4)
    } finally { $stream.Dispose() }
}
$X64 = 0x8664

function Find-AmongUs {
    if ($env:SPECIALROLES_GAME_DIR) { return $env:SPECIALROLES_GAME_DIR }   # nur zum Testen
    $candidates = @()
    $steam = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamPath
    if ($steam) {
        $candidates += Join-Path $steam 'steamapps\common\Among Us'
        $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
        if (Test-Path $vdf) {
            foreach ($m in [regex]::Matches((Get-Content $vdf -Raw), '"path"\s+"([^"]+)"')) {
                $candidates += Join-Path ($m.Groups[1].Value -replace '\\\\', '\') 'steamapps\common\Among Us'
            }
        }
    }
    foreach ($dir in $candidates | Select-Object -Unique) {
        if (Test-Path (Join-Path $dir 'Among Us.exe')) { return $dir }
    }
    Warn 'Among Us wurde nicht automatisch gefunden.'
    Write-Host '      Steam -> Rechtsklick auf Among Us -> Verwalten -> Lokale Dateien durchsuchen,'
    Write-Host '      dann oben die Adresse kopieren und hier einfügen.'
    while ($true) {
        $dir = (Read-Host '      Among-Us-Ordner').Trim().Trim('"')
        if ($dir -and (Test-Path (Join-Path $dir 'Among Us.exe'))) { return $dir }
        Warn 'Dort liegt keine "Among Us.exe". Nochmal versuchen.'
    }
}

function Wait-GameClosed {
    while (Get-Process 'Among Us' -ErrorAction SilentlyContinue) {
        Warn 'Among Us läuft noch. Bitte schließen und dann Enter drücken.'
        [void](Read-Host)
    }
}

function Install-BepInEx([string]$game) {
    $winhttp = Join-Path $game 'winhttp.dll'
    $coreclr = Join-Path $game 'dotnet\coreclr.dll'
    $ready = (Test-Path $winhttp) -and (Test-Path $coreclr) -and (Test-Path (Join-Path $game 'BepInEx\core\BepInEx.Core.dll')) `
        -and (Get-Machine $winhttp) -eq $X64 -and (Get-Machine $coreclr) -eq $X64
    if ($ready) { Ok 'BepInEx (64-Bit) ist schon installiert.'; return }

    $temp = Join-Path $env:TEMP "specialroles-bepinex-$PID"
    New-Item -ItemType Directory -Force $temp | Out-Null
    try {
        $zip = Join-Path $temp 'bepinex.zip'
        if ($env:SPECIALROLES_BEPINEX_ZIP) { Copy-Item $env:SPECIALROLES_BEPINEX_ZIP $zip }   # nur zum Testen
        else { Write-Host '      Lade BepInEx herunter (ca. 34 MB)...'; Invoke-WebRequest $BepInExUrl -OutFile $zip -UseBasicParsing }
        if ((Get-FileHash $zip -Algorithm SHA256).Hash -ne $BepInExSha256) { Fail 'Der BepInEx-Download ist beschädigt oder wurde verändert. Bitte später nochmal versuchen.' }
        Expand-Archive $zip (Join-Path $temp 'x') -Force
        $new = Join-Path $temp 'x'

        # Vorhandenes (z.B. altes 32-Bit-)BepInEx sichern statt löschen
        $old = @('winhttp.dll', 'doorstop_config.ini', '.doorstop_version', 'changelog.txt', 'dotnet', 'BepInEx\core', 'BepInEx\interop', 'BepInEx\cache') |
            Where-Object { Test-Path (Join-Path $game $_) }
        if ($old) {
            $backup = Join-Path $game ('_backup_bepinex_' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
            foreach ($item in $old) {
                $target = Join-Path $backup $item
                New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
                Move-Item (Join-Path $game $item) $target
            }
            Ok "Altes BepInEx gesichert in $(Split-Path $backup -Leaf)"
        }
        foreach ($item in 'winhttp.dll', 'doorstop_config.ini', '.doorstop_version', 'changelog.txt', 'dotnet') {
            Copy-Item (Join-Path $new $item) (Join-Path $game $item) -Recurse -Force
        }
        New-Item -ItemType Directory -Force (Join-Path $game 'BepInEx\plugins') | Out-Null
        Copy-Item (Join-Path $new 'BepInEx\core') (Join-Path $game 'BepInEx\core') -Recurse -Force
        if (-not (Test-Path (Join-Path $game 'BepInEx\patchers'))) { Copy-Item (Join-Path $new 'BepInEx\patchers') (Join-Path $game 'BepInEx\patchers') -Recurse }
        Ok 'BepInEx 6.0.0-be.735 (64-Bit) installiert.'
    } finally {
        Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Install-Mod([string]$game) {
    $plugins = Join-Path $game 'BepInEx\plugins'
    New-Item -ItemType Directory -Force $plugins | Out-Null
    $dll = Join-Path $plugins $ModDll
    $fresh = "$dll.new"
    if ($env:SPECIALROLES_MOD_DLL) {   # nur zum Testen
        Copy-Item $env:SPECIALROLES_MOD_DLL $fresh; $version = 'lokal'
    } else {
        # Neuestes Release inkl. Testversionen (releases/latest würde Pre-Releases überspringen)
        try {
            $releases = Invoke-RestMethod "https://api.github.com/repos/$Repo/releases?per_page=20" -Headers @{ 'User-Agent' = 'SpecialRoles-Installer' }
        } catch { Fail "Konnte die neueste Version nicht von GitHub abrufen: $($_.Exception.Message)" }
        $release = $releases | Where-Object { -not $_.draft -and ($_.assets | Where-Object name -eq $ModDll) } | Select-Object -First 1
        if (-not $release) { Fail "Auf GitHub gibt es noch kein Release mit $ModDll." }
        $asset = $release.assets | Where-Object name -eq $ModDll | Select-Object -First 1
        Invoke-WebRequest $asset.browser_download_url -OutFile $fresh -UseBasicParsing -Headers @{ 'User-Agent' = 'SpecialRoles-Installer' }
        $version = $release.tag_name
        if ($release.prerelease) { $version += ' (Testversion)' }
    }
    $head = [IO.File]::ReadAllBytes($fresh)
    if ($head.Length -lt 10000 -or $head[0] -ne 0x4D -or $head[1] -ne 0x5A) { Remove-Item $fresh; Fail 'Der Mod-Download ist keine gültige DLL.' }
    Remove-Item "$dll.old" -ErrorAction SilentlyContinue
    Move-Item $fresh $dll -Force
    Ok "SpecialRoles $version installiert."
}

function Uninstall([string]$game) {
    $plugins = Join-Path $game 'BepInEx\plugins'
    Remove-Item (Join-Path $plugins "$ModDll*") -Force -ErrorAction SilentlyContinue
    Ok 'SpecialRoles entfernt.'
    $others = Get-ChildItem $plugins -Filter '*.dll' -Recurse -ErrorAction SilentlyContinue
    if ($others) { Warn 'Andere Mods gefunden (z.B. CrewStats) - BepInEx bleibt aktiv.'; return }
    Remove-Item (Join-Path $game 'winhttp.dll') -Force -ErrorAction SilentlyContinue
    Ok 'BepInEx deaktiviert - Among Us startet wieder ganz normal.'
}

# ==========================================================================================

Write-Host ''
Write-Host '  ================================' -ForegroundColor Red
Write-Host '   SpecialRoles für Among Us' -ForegroundColor White
Write-Host '  ================================' -ForegroundColor Red
Write-Host '  30 neue Rollen für Among Us. Alle Spieler der Lobby brauchen dieselbe Version.'
Write-Host '  Die Rollen erklärt der Discord-Bot mit /special-rolle.'
Write-Host ''
Write-Host '  [1] Installieren / Aktualisieren  (Enter)'
Write-Host '  [2] Deinstallieren'
$choice = (Read-Host '  Auswahl').Trim()

Step 'Suche Among Us...'
$game = Find-AmongUs
Ok $game
Wait-GameClosed

if ($choice -eq '2') {
    Step 'Deinstalliere...'
    Uninstall $game
    exit 0
}

if ((Get-Machine (Join-Path $game 'Among Us.exe')) -ne $X64) {
    Fail 'Dein Among Us ist veraltet (32-Bit). Bitte zuerst in Steam aktualisieren.'
}

Step 'BepInEx (Mod-Loader)'
Install-BepInEx $game

Step 'SpecialRoles-Mod'
Install-Mod $game

Write-Host ''
Write-Host '  Fertig!' -ForegroundColor Green
Write-Host '  - Der erste Start dauert 1-2 Minuten (BepInEx bereitet alles vor).'
Write-Host '  - Deine Rolle steht im Spiel unter deinem Namen und oben in der Aufgabenliste.'
Write-Host '  - Neue Version? Einfach diesen Installer nochmal starten.'
if ((Read-Host "`n  Among Us jetzt starten? (j/n)").Trim() -match '^(j|ja|y|yes)$') { Start-Process 'steam://rungameid/945360' }
