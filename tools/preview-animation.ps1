param(
    [string]$OutputDirectory = 'work/solar-b5-preview',
    [string]$FfmpegPath = ''
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not [IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectRoot $OutputDirectory
}
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
$runName = 'preview-{0}-{1}' -f (Get-Date -Format 'yyyyMMdd-HHmmss'), ([Guid]::NewGuid().ToString('N').Substring(0, 8))
$runDirectory = Join-Path $outputRoot $runName
$dotnet = Join-Path $projectRoot 'local/toolchain/dotnet/dotnet.exe'
if (-not (Test-Path -LiteralPath $dotnet)) { throw 'Run ./tools/bootstrap.ps1 first.' }
[xml]$localProps = Get-Content -LiteralPath (Join-Path $projectRoot 'local.props') -Raw
$godotPath = [string]$localProps.Project.PropertyGroup.GodotPath
$gameData = Join-Path ([string]$localProps.Project.PropertyGroup.Sts2Path) 'data_sts2_windows_x86_64'
$modDll = Join-Path $projectRoot 'dist/Shenshen/Shenshen.dll'
$baseDll = Join-Path $projectRoot 'dist/BaseLib/BaseLib.dll'
foreach ($required in @($godotPath, (Join-Path $gameData 'sts2.dll'), $modDll, $baseDll)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Missing preview dependency: $required. Build/pack the mod before previewing."
    }
}
if ($FfmpegPath) {
    if (-not [IO.Path]::IsPathRooted($FfmpegPath)) { $FfmpegPath = Join-Path $projectRoot $FfmpegPath }
    if (-not (Test-Path -LiteralPath $FfmpegPath -PathType Leaf)) { throw "ffmpeg not found: $FfmpegPath" }
} else {
    $ffmpegCommand = Get-Command ffmpeg -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($ffmpegCommand) { $FfmpegPath = $ffmpegCommand.Source }
}

# Every run is a new directory; retain previous previews and failed evidence.
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'animation-preview') -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $runDirectory
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'Shenshen') -Destination $runDirectory -Recurse
$dependencies = Join-Path $runDirectory 'dependencies'
New-Item -ItemType Directory -Path $dependencies -Force | Out-Null
Copy-Item -LiteralPath $modDll, $baseDll -Destination $dependencies
New-Item -ItemType File -Path (Join-Path $dependencies '.gdignore') -Force | Out-Null
$manifest = [ordered]@{
    description = 'Independent Godot GPU preview; not game footage or gameplay verification.'
    inputs = 'Snapshot of dist DLLs and current Shenshen source resources; no linked production C# source.'
    createdUtc = [DateTime]::UtcNow.ToString('o')
    directory = $runDirectory
    godotPath = $godotPath
    gameAssembly = (Get-FileHash -LiteralPath (Join-Path $gameData 'sts2.dll')).Hash
    modAssembly = (Get-FileHash -LiteralPath (Join-Path $dependencies 'Shenshen.dll')).Hash
    baseAssembly = (Get-FileHash -LiteralPath (Join-Path $dependencies 'BaseLib.dll')).Hash
    resources = @(Get-ChildItem -LiteralPath (Join-Path $runDirectory 'Shenshen') -Recurse -File | ForEach-Object {
        @{ path = $_.FullName.Substring($runDirectory.Length + 1); sha256 = (Get-FileHash -LiteralPath $_.FullName).Hash }
    })
    state = 'prepared'
    video = 'animation-preview.avi'
}
$manifestPath = Join-Path $runDirectory 'preview-manifest.json'
function Save-PreviewManifest { $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8 }
Save-PreviewManifest

function Invoke-PreviewGodot([string]$Name, [string[]]$Arguments) {
    # Start-Process joins ArgumentList; quote each native argument to preserve paths with spaces.
    $quotedArguments = @($Arguments | ForEach-Object { '"' + $_ + '"' })
    $process = Start-Process -FilePath $godotPath -ArgumentList ($quotedArguments -join ' ') -WindowStyle Hidden -Wait -PassThru `
        -RedirectStandardOutput (Join-Path $runDirectory "$Name.log") `
        -RedirectStandardError (Join-Path $runDirectory "$Name-error.log")
    if ($process.ExitCode -ne 0) { throw "Godot $Name failed ($($process.ExitCode)); inspect $runDirectory." }
}

$environmentNames = @('DOTNET_ROOT', 'DOTNET_CLI_HOME', 'NUGET_PACKAGES', 'PATH',
    'DOTNET_CLI_TELEMETRY_OPTOUT', 'DOTNET_SKIP_FIRST_TIME_EXPERIENCE',
    'APPDATA', 'LOCALAPPDATA', 'SHENSHEN_GAME_DATA', 'SHENSHEN_PREVIEW_MOVIE')
$previousEnvironment = @{}
foreach ($name in $environmentNames) { $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
    $env:DOTNET_ROOT = Join-Path $projectRoot 'local/toolchain/dotnet'
    $env:DOTNET_CLI_HOME = Join-Path $projectRoot 'local/dotnet-cli'
    $env:NUGET_PACKAGES = Join-Path $projectRoot 'local/nuget-packages'
    $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
    $env:APPDATA = Join-Path $runDirectory 'userdata/appdata'
    $env:LOCALAPPDATA = Join-Path $runDirectory 'userdata/localappdata'
    $env:SHENSHEN_GAME_DATA = $gameData
    $env:SHENSHEN_PREVIEW_MOVIE = '0'
    New-Item -ItemType Directory -Path $env:APPDATA, $env:LOCALAPPDATA, $env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES -Force | Out-Null
    New-Item -ItemType File -Path (Join-Path $runDirectory 'userdata/.gdignore') -Force | Out-Null

    & $dotnet build (Join-Path $runDirectory 'AnimationPreview.csproj') -c Debug "-p:GameData=$gameData" *> (Join-Path $runDirectory 'build.log')
    if ($LASTEXITCODE -ne 0) { throw "Preview compilation failed; inspect $runDirectory/build.log." }
    Invoke-PreviewGodot 'import' @('--headless', '--editor', '--path', $runDirectory, '--import', '--quit')
    Invoke-PreviewGodot 'frames' @('--path', $runDirectory, '--quit-after', '1800')
    if (-not (Select-String -LiteralPath (Join-Path $runDirectory 'frames.log') -SimpleMatch 'SHENSHEN_PREVIEW_FRAMES_DONE' -Quiet)) {
        throw 'Godot exited before the static preview frames were completed.'
    }
    $manifest.state = 'frames-complete'
    Save-PreviewManifest

    $env:SHENSHEN_PREVIEW_MOVIE = '1'
    $aviPath = Join-Path $runDirectory 'animation-preview.avi'
    Invoke-PreviewGodot 'movie' @('--path', $runDirectory, '--write-movie', $aviPath, '--fixed-fps', '30', '--quit-after', '1800')
    if (-not (Test-Path -LiteralPath $aviPath) -or
        -not (Select-String -LiteralPath (Join-Path $runDirectory 'movie.log') -SimpleMatch 'SHENSHEN_PREVIEW_MOVIE_DONE' -Quiet)) {
        throw 'Godot exited without completing the 14.8-second animation preview.'
    }
    if ($FfmpegPath) {
        $mp4Path = Join-Path $runDirectory 'animation-preview.mp4'
        & $FfmpegPath -nostdin -y -i $aviPath -an -c:v libx264 -preset medium -crf 18 -pix_fmt yuv420p -movflags +faststart $mp4Path *> (Join-Path $runDirectory 'ffmpeg.log')
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $mp4Path)) {
            throw "ffmpeg failed; the original AVI is retained at $aviPath."
        }
        $manifest.video = 'animation-preview.mp4'
    }
    $manifest.state = 'complete'
    Save-PreviewManifest
} catch {
    $manifest.state = 'failed'
    $manifest['failure'] = $_.Exception.Message
    Save-PreviewManifest
    throw
} finally {
    foreach ($name in $environmentNames) { [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process') }
}
Write-Output "Independent preview complete: $runDirectory"
Write-Output "Video: $(Join-Path $runDirectory $manifest.video)"
Write-Output 'Static PNGs, source hashes, isolated user data and logs are retained beside the video. This is not in-game footage.'
