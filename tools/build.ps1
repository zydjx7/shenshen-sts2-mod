param(
    [ValidateSet('build', 'pack')][string]$Action = 'build',
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$BuildArguments = @()
)
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
@($env:DOTNET_CLI_HOME, $env:NUGET_PACKAGES) | ForEach-Object { New-Item -ItemType Directory -Path $_ -Force | Out-Null }
New-Item -ItemType File -Path (Join-Path $projectRoot 'local/.gdignore') -Force | Out-Null
$dotnetArguments = @('build', 'Shenshen.csproj')
if ($Action -eq 'pack') { $dotnetArguments = @('publish', 'Shenshen.csproj', '-c', 'Release') }
Push-Location $projectRoot
try {
    & $dotnet @dotnetArguments @BuildArguments
    $buildExitCode = $LASTEXITCODE
} finally {
    Pop-Location
}
exit $buildExitCode
