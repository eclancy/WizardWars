#!/usr/bin/env bash
# Publish a new version of Wizard Wars. Every copy of the game picks it up the next time it starts.
#
#   bash tools/release/release.sh 0.2.0
#
# Commits the version bump, tags it, exports the Windows build, and makes a GitHub Release with
# the full zip, the small patch zip and version.json. ecec.dev/wizard-wars/version.json
# redirects to the newest release's version.json, so nothing on ecec.dev needs deploying. See
# docs/releasing.md.
set -euo pipefail

VERSION="${1:-}"
if [[ ! "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
	echo "usage: bash tools/release/release.sh X.Y.Z" >&2
	exit 1
fi

cd "$(dirname "$0")/../.."
# Same default as tools/check.sh. package.py reads the engine version from this name.
export GODOT_BIN="${GODOT_BIN:-C:/Program Files (x86)/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64_console.exe}"
[ -f "$GODOT_BIN" ] || { echo "Godot not found at: $GODOT_BIN - set GODOT_BIN" >&2; exit 1; }

if [ "$(git rev-parse --abbrev-ref HEAD)" != "master" ]; then
	echo "releases are made from master" >&2
	exit 1
fi
if [ -n "$(git status --porcelain)" ]; then
	echo "commit or stash your changes first - a release is built from what is committed" >&2
	exit 1
fi
if git rev-parse -q --verify "refs/tags/v$VERSION" >/dev/null; then
	echo "v$VERSION already exists" >&2
	exit 1
fi

CURRENT=$(sed -n 's/^config\/version="\(.*\)"$/\1/p' project.godot)
echo "Wizard Wars $CURRENT -> $VERSION"

# The version lives in project.godot, which is where the running game reads it from.
sed -i "s/^config\/version=\".*\"$/config\/version=\"$VERSION\"/" project.godot

dotnet build WizardWars.sln -nologo -v q
rm -rf build/windows build/release
mkdir -p build/windows
# Godot does not create the export folder itself, and on an export error it can sit there
# instead of exiting - hence the timeout.
timeout 900 "$GODOT_BIN" --headless --path . --export-release "Windows Desktop" build/windows/WizardWars.exe \
	> build/export.log 2>&1 || { tail -20 build/export.log; echo "export failed - see build/export.log" >&2; git checkout project.godot; exit 1; }
[ -f build/windows/WizardWars.pck ] || { echo "export produced no .pck - see build/export.log" >&2; git checkout project.godot; exit 1; }

python tools/release/package.py "$VERSION"

git diff --quiet project.godot || git commit -q -m "Release v$VERSION" project.godot
git tag "v$VERSION"
git push -q origin master "v$VERSION"

gh release create "v$VERSION" --repo eclancy/WizardWars \
	build/release/WizardWars-windows.zip build/release/WizardWars-patch.zip build/release/version.json \
	--title "Wizard Wars $VERSION" \
	--notes "Download **WizardWars-windows.zip**, unzip it, and run WizardWars.exe. It keeps itself up to date from then on. Download page: https://ecec.dev/wizard-wars/download"

echo "Released v$VERSION - every copy updates the next time it starts."
