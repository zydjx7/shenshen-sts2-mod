$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path -Parent $PSScriptRoot
$env:DOTNET_ROOT = Join-Path $projectRoot 'local/toolchain/dotnet'
$env:DOTNET_CLI_HOME = Join-Path $projectRoot 'local/dotnet-cli'
$env:NUGET_PACKAGES = Join-Path $projectRoot 'local/nuget-packages'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$dotnet = Join-Path $env:DOTNET_ROOT 'dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) { throw 'Run ./tools/bootstrap.ps1 first.' }
[xml]$localProps = Get-Content -LiteralPath (Join-Path $projectRoot 'local.props') -Raw
$gamePath = [string]$localProps.Project.PropertyGroup.Sts2Path
$godotPath = [string]$localProps.Project.PropertyGroup.GodotPath
$gameData = Join-Path $gamePath 'data_sts2_windows_x86_64'
if (-not (Test-Path -LiteralPath (Join-Path $gameData 'sts2.dll'))) { throw 'Windows game assembly not found in local.props Sts2Path.' }

# Source is versioned; the engine project, binaries, logs and isolated user data stay local.
& (Join-Path $PSScriptRoot 'build.ps1') pack
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$testProject = Join-Path $projectRoot 'work/headless-tests'
New-Item -ItemType Directory -Path $testProject -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'tests') -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $testProject -Force
}
Copy-Item -LiteralPath (Join-Path $gamePath 'release_info.json') -Destination $testProject -Force
$env:SHENSHEN_REPO_ROOT = $projectRoot
$env:SHENSHEN_GAME_PATH = $gamePath
& $dotnet build (Join-Path $testProject 'Harness.csproj') "-p:RepositoryRoot=$projectRoot" "-p:GameData=$gameData"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$logFile = Join-Path $testProject 'run.log'
$previousAppData = $env:APPDATA
$previousLocalAppData = $env:LOCALAPPDATA
try {
    $env:APPDATA = Join-Path $projectRoot 'local/regression-appdata'
    $env:LOCALAPPDATA = Join-Path $projectRoot 'local/regression-localappdata'
    New-Item -ItemType Directory -Path $env:APPDATA,$env:LOCALAPPDATA -Force | Out-Null
    & $godotPath --headless --path $testProject --log-file $logFile --quit-after 1800
    $testExitCode = $LASTEXITCODE
} finally {
    $env:APPDATA = $previousAppData
    $env:LOCALAPPDATA = $previousLocalAppData
}
if ($testExitCode -ne 0) { exit $testExitCode }
if (-not (Select-String -LiteralPath $logFile -SimpleMatch 'SHENSHEN_HEADLESS_PASS')) {
    throw 'The headless process exited without completing the regression checks.'
}
