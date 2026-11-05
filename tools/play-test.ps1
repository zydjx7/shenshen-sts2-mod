param(
    [ValidateSet('prepare', 'start', 'verify')][string]$Action = 'prepare'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path -Parent $PSScriptRoot
$testRoot = Join-Path $projectRoot 'local/play-test'
$snapshotPath = Join-Path $testRoot 'daily-saves-before.json'
$launchPath = Join-Path $testRoot 'launch.json'
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null

function Assert-GameStopped {
    if (Get-Process -Name 'SlayTheSpire2' -ErrorAction SilentlyContinue) {
        throw 'Exit Slay the Spire 2 before preparing or verifying the test.'
    }
}

function Get-DailySaveRoots {
    @(
        (Join-Path $env:APPDATA 'SlayTheSpire2/steam'),
        (Join-Path $env:APPDATA 'SlayTheSpire2/default'),
        (Join-Path $env:APPDATA 'Godot/app_userdata/sts2/steam'),
        (Join-Path $env:APPDATA 'Godot/app_userdata/sts2/default')
    )
    $steam = Get-ItemProperty -LiteralPath 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue
    if ($steam -and $steam.SteamPath) {
        $userdata = Join-Path $steam.SteamPath 'userdata'
        if (Test-Path -LiteralPath $userdata) {
            Get-ChildItem -LiteralPath $userdata -Directory | ForEach-Object {
                Join-Path $_.FullName '2868840'
            }
        }
    }
}

function Get-SaveSnapshot([string[]]$Roots) {
    foreach ($root in $Roots) {
        if (Test-Path -LiteralPath $root) {
            Get-ChildItem -LiteralPath $root -Recurse -File | Sort-Object FullName | ForEach-Object {
                [PSCustomObject]@{
                    path = $_.FullName
                    length = $_.Length
                    sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
                }
            }
        }
    }
}

Assert-GameStopped
if ($Action -eq 'verify') {
    if (-not (Test-Path -LiteralPath $snapshotPath)) { throw 'Run prepare or start before verify.' }
    $baseline = Get-Content -LiteralPath $snapshotPath -Raw | ConvertFrom-Json
    $after = @(Get-SaveSnapshot @($baseline.roots))
    $beforeSignatures = @($baseline.files | ForEach-Object { "$($_.path)|$($_.length)|$($_.sha256)" })
    $afterSignatures = @($after | ForEach-Object { "$($_.path)|$($_.length)|$($_.sha256)" })
    $changes = @(Compare-Object $beforeSignatures $afterSignatures)
    $report = [PSCustomObject]@{
        verifiedUtc = [DateTime]::UtcNow.ToString('o')
        dailySavesUnchanged = ($changes.Count -eq 0)
        checkedFiles = $after.Count
        changes = $changes
        isolatedFiles = @(Get-SaveSnapshot @((Join-Path $testRoot 'appdata/SlayTheSpire2'), (Join-Path $testRoot 'appdata/Godot/app_userdata/sts2')))
    }
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $testRoot 'verification.json') -Encoding UTF8
    if ($changes.Count -gt 0) { throw 'Daily save hashes changed. See local/play-test/verification.json.' }
    Write-Host "Daily saves unchanged ($($after.Count) file hashes). Test files: $($report.isolatedFiles.Count)."
    exit 0
}

[xml]$props = Get-Content -LiteralPath (Join-Path $projectRoot 'local.props') -Raw
$gameDir = [string]$props.Project.PropertyGroup.Sts2Path
$gameExecutable = Join-Path $gameDir 'SlayTheSpire2.exe'
if (-not (Test-Path -LiteralPath $gameExecutable)) { throw 'Check Sts2Path in local.props.' }
$modsDir = Join-Path $gameDir 'mods'
$otherMods = @(Get-ChildItem -LiteralPath $modsDir -Directory -ErrorAction SilentlyContinue | Where-Object Name -NotIn @('BaseLib', 'Shenshen'))
if ($otherMods.Count -gt 0) { throw 'Other mods are installed. Use a separate game copy to test this mod alone.' }
foreach ($name in @('BaseLib', 'Shenshen')) {
    foreach ($extension in @('dll', 'json', 'pck')) {
        if (-not (Test-Path -LiteralPath (Join-Path $projectRoot "dist/$name/$name.$extension"))) {
            throw 'Run ./tools/build.ps1 pack before deploying the test.'
        }
    }
}
$dailyRoots = @(Get-DailySaveRoots)
[PSCustomObject]@{
    capturedUtc = [DateTime]::UtcNow.ToString('o')
    roots = $dailyRoots
    files = @(Get-SaveSnapshot $dailyRoots)
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $snapshotPath -Encoding UTF8

$backupDir = Join-Path $testRoot ('mod-backups/' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfff'))
foreach ($name in @('BaseLib', 'Shenshen')) {
    $destination = Join-Path $modsDir $name
    if (Test-Path -LiteralPath $destination) {
        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
        Copy-Item -LiteralPath $destination -Destination $backupDir -Recurse
    }
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $projectRoot "dist/$name") -File | Copy-Item -Destination $destination -Force
}

$isolatedAppData = Join-Path $testRoot 'appdata'
New-Item -ItemType Directory -Path $isolatedAppData -Force | Out-Null
$logPath = Join-Path $testRoot 'game.log'
# Verified in this game's NGame.InitializePlatform: off skips Steam initialization,
# so SaveManager uses GodotFileIo directly and cannot create CloudSaveStore.
# MegaDot 4.5.1-m.12 was probed to confirm Windows user:// follows process APPDATA.
$arguments = '--force-steam=off --shenshen-test --windowed --resolution 1280x720 --log-file "' + $logPath + '"'
$launch = [PSCustomObject]@{
    preparedUtc = [DateTime]::UtcNow.ToString('o')
    executable = $gameExecutable
    arguments = $arguments
    appData = $isolatedAppData
    processId = $null
}
if ($Action -eq 'start') {
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $gameExecutable
    $startInfo.WorkingDirectory = $gameDir
    $startInfo.Arguments = $arguments
    $startInfo.UseShellExecute = $false
    $startInfo.EnvironmentVariables['APPDATA'] = $isolatedAppData
    $process = [Diagnostics.Process]::Start($startInfo)
    $launch.processId = $process.Id
    Write-Host "Started isolated test process $($process.Id). Exit the game, then run ./tools/play-test.ps1 verify."
} else {
    Write-Host 'Deployed test build and recorded daily save hashes. Run ./tools/play-test.ps1 start to launch.'
}
$launch | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $launchPath -Encoding UTF8
