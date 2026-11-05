param(
    [string]$Sts2Path = '',
    [ValidateSet('arm64', 'x86_64')][string]$Sts2Architecture = 'x86_64'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'bootstrap.ps1 supports Windows only; use bootstrap.sh on macOS.'
}
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot
$lock = Get-Content -LiteralPath (Join-Path $projectRoot 'toolchain.lock.json') -Raw | ConvertFrom-Json
$localDir = Join-Path $projectRoot 'local'
$dotnetDir = Join-Path $localDir 'toolchain/dotnet'
$megaDir = Join-Path $localDir 'toolchain/megadot'
$env:DOTNET_ROOT = $dotnetDir
$env:DOTNET_CLI_HOME = Join-Path $localDir 'dotnet-cli'
$env:NUGET_PACKAGES = Join-Path $localDir 'nuget-packages'
$env:PATH = "$dotnetDir;$env:PATH"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
@($dotnetDir, $megaDir, $env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES) | ForEach-Object {
    New-Item -ItemType Directory -Path $_ -Force | Out-Null
}
New-Item -ItemType File -Path (Join-Path $localDir '.gdignore') -Force | Out-Null
$tempDir = Join-Path $localDir ('toolchain/.bootstrap-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tempDir | Out-Null
try {
    $dotnet = Join-Path $dotnetDir 'dotnet.exe'
    $sdkInstalled = $false
    if (Test-Path -LiteralPath $dotnet) {
        $sdkList = & $dotnet --list-sdks
        if ($LASTEXITCODE -ne 0) { throw "dotnet --list-sdks failed with exit code $LASTEXITCODE." }
        $sdkInstalled = [bool]($sdkList | Where-Object { $_ -match ('^' + [Regex]::Escape($lock.sdkVersion) + '\s') })
    }
    if ($sdkInstalled) {
        Write-Host ".NET SDK $($lock.sdkVersion) already installed."
    } else {
        $installer = Join-Path $tempDir 'dotnet-install.ps1'
        Invoke-WebRequest -UseBasicParsing -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
        # The official installer can call exit; isolate it from this bootstrap process.
        $installerHost = Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
        if (-not (Test-Path -LiteralPath $installerHost)) { throw 'Windows PowerShell was not found for the .NET installer.' }
        & $installerHost -NoLogo -NoProfile -NonInteractive -File $installer -Version $lock.sdkVersion -Architecture x64 -InstallDir $dotnetDir -NoPath
        $installerExitCode = $LASTEXITCODE
        if ($installerExitCode -ne 0) { throw ".NET installer failed with exit code $installerExitCode." }
    }
    $actualSdk = & $dotnet --version
    if ($LASTEXITCODE -ne 0 -or $actualSdk -ne $lock.sdkVersion) { throw 'Installed .NET SDK does not match toolchain.lock.json.' }

    function Find-MegaDotExecutable {
        $executables = @(Get-ChildItem -LiteralPath $megaDir -Filter '*.exe' -File -Recurse)
        $console = @($executables | Where-Object { $_.Name -like '*console.exe' } | Sort-Object FullName)
        if ($console.Count -gt 0) { return $console[0].FullName }
        $editors = @($executables | Where-Object { $_.Name -match '(?i)(megadot|godot)' } | Sort-Object FullName)
        if ($editors.Count -gt 0) { return $editors[0].FullName }
        return $null
    }
    $megaExecutable = Find-MegaDotExecutable
    $versionMarker = Join-Path $megaDir '.installed-version'
    $installedMegaVersion = ''
    if (Test-Path -LiteralPath $versionMarker) { $installedMegaVersion = (Get-Content -LiteralPath $versionMarker -Raw).Trim() }
    if ($megaExecutable -and $installedMegaVersion -eq $lock.megaDotVersion) {
        Write-Host "MegaDot $($lock.megaDotVersion) already installed."
    } else {
        $zipName = "megadot-$($lock.megaDotVersion)-windows-x86_64-llvm-editor-csharp.zip"
        $downloadUrl = "https://megadot.megacrit.com/$($lock.megaDotVersion)/$zipName"
        $archive = Join-Path $tempDir 'megadot.zip'
        $checksum = Join-Path $tempDir 'megadot.zip.sha256'
        Invoke-WebRequest -UseBasicParsing -Uri $downloadUrl -OutFile $archive
        Invoke-WebRequest -UseBasicParsing -Uri "$downloadUrl.sha256" -OutFile $checksum
        $expectedSha = ((Get-Content -LiteralPath $checksum -Raw).Trim() -split '\s+')[0]
        if ($expectedSha -notmatch '^[0-9a-fA-F]{64}$') { throw 'Official MegaDot SHA256 file is invalid.' }
        $actualSha = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
        if ($actualSha -ne $expectedSha) { throw 'MegaDot SHA256 verification failed; archive was not installed.' }
        $extracted = Join-Path $tempDir 'extracted'
        Expand-Archive -LiteralPath $archive -DestinationPath $extracted
        if (@(Get-ChildItem -LiteralPath $extracted -Filter '*.exe' -File -Recurse).Count -eq 0) { throw 'MegaDot archive contains no executable.' }
        Remove-Item -LiteralPath $megaDir -Recurse -Force
        Move-Item -LiteralPath $extracted -Destination $megaDir
        [IO.File]::WriteAllText($versionMarker, $lock.megaDotVersion + [Environment]::NewLine)
        $megaExecutable = Find-MegaDotExecutable
        if (-not $megaExecutable) { throw 'No MegaDot editor executable found after extraction.' }
    }

    $localProps = Join-Path $projectRoot 'local.props'
    if (-not (Test-Path -LiteralPath $localProps)) {
        if (-not $Sts2Path) {
            $steamBase = ${env:ProgramFiles(x86)}
            if (-not $steamBase) { $steamBase = $env:ProgramFiles }
            $Sts2Path = Join-Path $steamBase 'Steam/steamapps/common/Slay the Spire 2'
        }
        $escapedMegaPath = [Security.SecurityElement]::Escape($megaExecutable)
        $escapedGamePath = [Security.SecurityElement]::Escape($Sts2Path)
        $content = @"
<Project>
  <PropertyGroup>
    <GodotPath>$escapedMegaPath</GodotPath>
    <Sts2Path>$escapedGamePath</Sts2Path>
    <Sts2Architecture>$Sts2Architecture</Sts2Architecture>
  </PropertyGroup>
</Project>
"@
        [IO.File]::WriteAllText($localProps, $content, (New-Object Text.UTF8Encoding($false)))
        Write-Host 'Created local.props; check Sts2Path if Steam uses another library.'
    } else {
        Write-Host 'Kept existing local.props.'
    }
    & $dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet tool restore failed with exit code $LASTEXITCODE." }
    Write-Host 'Toolchain ready. Next: ./tools/build.ps1 build, or ./tools/build.ps1 pack'
} finally {
    Remove-Item -LiteralPath $tempDir -Recurse -Force
}
