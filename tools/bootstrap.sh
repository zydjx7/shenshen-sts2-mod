#!/bin/bash
# Install the pinned toolchain inside this checkout. macOS only.
set -euo pipefail

die() { printf 'Error: %s\n' "$*" >&2; exit 1; }
[[ "$(uname -s)" == Darwin ]] || die 'bootstrap.sh supports macOS only; use bootstrap.ps1 on Windows.'

project_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$project_root"
lock_file="$project_root/toolchain.lock.json"
sdk_version="$(/usr/bin/plutil -extract sdkVersion raw -o - "$lock_file")"
mega_version="$(/usr/bin/plutil -extract megaDotVersion raw -o - "$lock_file")"
local_dir="$project_root/local"
dotnet_dir="$local_dir/toolchain/dotnet"
mega_dir="$local_dir/toolchain/megadot"
mkdir -p "$dotnet_dir" "$mega_dir" "$local_dir/dotnet-cli" "$local_dir/nuget-packages"
touch "$local_dir/.gdignore"
export DOTNET_ROOT="$dotnet_dir"
export DOTNET_CLI_HOME="$local_dir/dotnet-cli"
export NUGET_PACKAGES="$local_dir/nuget-packages"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

case "$(uname -m)" in
    arm64) dotnet_arch=arm64; game_arch=arm64 ;;
    x86_64) dotnet_arch=x64; game_arch=x86_64 ;;
    *) die "Unsupported Mac architecture: $(uname -m)" ;;
esac
game_arch="${STS2_ARCHITECTURE:-$game_arch}"
[[ "$game_arch" == arm64 || "$game_arch" == x86_64 ]] || die 'STS2_ARCHITECTURE must be arm64 or x86_64.'

temp_dir="$(mktemp -d "$local_dir/toolchain/.bootstrap.XXXXXX")"
trap 'rm -rf "$temp_dir"' EXIT
if [[ -x "$dotnet_dir/dotnet" ]] && "$dotnet_dir/dotnet" --list-sdks | awk -v v="$sdk_version" '$1 == v { found=1 } END { exit !found }'; then
    printf '.NET SDK %s already installed.\n' "$sdk_version"
else
    curl --fail --location --silent --show-error https://dot.net/v1/dotnet-install.sh -o "$temp_dir/dotnet-install.sh"
    bash "$temp_dir/dotnet-install.sh" --version "$sdk_version" --architecture "$dotnet_arch" --install-dir "$dotnet_dir" --no-path
fi
[[ "$("$dotnet_dir/dotnet" --version)" == "$sdk_version" ]] || die 'Installed .NET SDK does not match toolchain.lock.json.'

find_mega_executable() {
    local candidate
    for candidate in "$mega_dir/MegaDot.app/Contents/MacOS/"*; do
        if [[ -f "$candidate" && -x "$candidate" ]]; then printf '%s\n' "$candidate"; return 0; fi
    done
    return 1
}
mega_executable="$(find_mega_executable || true)"
installed_mega_version=''
if [[ -f "$mega_dir/.installed-version" ]]; then installed_mega_version="$(cat "$mega_dir/.installed-version")"; fi
if [[ -n "$mega_executable" && "$installed_mega_version" != "$mega_version" ]]; then
    runtime_version="$("$mega_executable" --version 2>/dev/null || true)"
    case "$runtime_version" in
        "$mega_version"*|"${mega_version/-/.}"*) installed_mega_version="$mega_version" ;;
    esac
fi
if [[ -n "$mega_executable" && "$installed_mega_version" == "$mega_version" ]]; then
    printf 'MegaDot %s already installed.\n' "$mega_version"
else
    zip_name="megadot-$mega_version-macos-universal-editor-csharp.zip"
    download_url="https://megadot.megacrit.com/$mega_version/$zip_name"
    curl --fail --location --show-error "$download_url" -o "$temp_dir/megadot.zip"
    curl --fail --location --silent --show-error "$download_url.sha256" -o "$temp_dir/megadot.zip.sha256"
    expected_sha="$(awk 'NR == 1 { print tolower($1) }' "$temp_dir/megadot.zip.sha256")"
    [[ "$expected_sha" =~ ^[0-9a-f]{64}$ ]] || die 'Official MegaDot SHA256 file is invalid.'
    actual_sha="$(shasum -a 256 "$temp_dir/megadot.zip" | awk '{print $1}')"
    [[ "$expected_sha" == "$actual_sha" ]] || die 'MegaDot SHA256 verification failed; archive was not installed.'
    /usr/bin/ditto -x -k "$temp_dir/megadot.zip" "$temp_dir/extracted"
    [[ -d "$temp_dir/extracted/MegaDot.app" ]] || die 'MegaDot archive does not contain MegaDot.app.'
    rm -rf "$mega_dir/MegaDot.app"
    mv "$temp_dir/extracted/MegaDot.app" "$mega_dir/MegaDot.app"
    printf '%s\n' "$mega_version" > "$mega_dir/.installed-version"
    mega_executable="$(find_mega_executable)" || die 'No MegaDot executable found in app bundle.'
fi

xml_escape() { printf '%s' "$1" | sed 's/\&/\&amp;/g; s/</\&lt;/g; s/>/\&gt;/g'; }
if [[ ! -e "$project_root/local.props" ]]; then
    game_path="${STS2_PATH:-$HOME/Library/Application Support/Steam/steamapps/common/Slay the Spire 2}"
    cat > "$project_root/local.props" <<EOF
<Project>
  <PropertyGroup>
    <GodotPath>$(xml_escape "$mega_executable")</GodotPath>
    <Sts2Path>$(xml_escape "$game_path")</Sts2Path>
    <Sts2Architecture>$game_arch</Sts2Architecture>
  </PropertyGroup>
</Project>
EOF
    printf 'Created local.props; check Sts2Path if Steam uses another library.\n'
else
    printf 'Kept existing local.props.\n'
fi
"$dotnet_dir/dotnet" tool restore
printf 'Toolchain ready. Next: ./tools/build.sh build, or ./tools/build.sh pack\n'
printf 'If macOS blocks MegaDot, review its approval in System Settings; this script does not change security settings.\n'
