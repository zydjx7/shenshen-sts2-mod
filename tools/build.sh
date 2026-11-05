#!/bin/bash
set -euo pipefail
project_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$project_root"
action="${1:-build}"
if [[ $# -gt 0 ]]; then shift; fi
case "$action" in build|pack) ;; *) printf 'Usage: %s [build|pack] [dotnet arguments...]\n' "$0" >&2; exit 2 ;; esac
export DOTNET_ROOT="$project_root/local/toolchain/dotnet"
export DOTNET_CLI_HOME="$project_root/local/dotnet-cli"
export NUGET_PACKAGES="$project_root/local/nuget-packages"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
[[ -x "$DOTNET_ROOT/dotnet" ]] || { printf 'Run ./tools/bootstrap.sh first.\n' >&2; exit 1; }
mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES"
touch "$project_root/local/.gdignore"
if [[ "$action" == pack ]]; then
    exec "$DOTNET_ROOT/dotnet" publish Shenshen.csproj -c Release "$@"
else
    # Match the configuration used by packages.lock.json and resource packaging.
    exec "$DOTNET_ROOT/dotnet" build Shenshen.csproj -c Release "$@"
fi
