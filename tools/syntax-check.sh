#!/usr/bin/env bash
# Compile every script under Assets/Scripts with Unity's own Roslyn, against Unity's
# assemblies, without touching Library/.
#
# Unlike tools/compile-check.sh this works WHILE THE EDITOR IS OPEN, because it never
# opens the project. It catches compile errors and warnings only: it does not import
# assets, so it cannot catch broken GUID references. Use compile-check.sh for that when
# the Editor is closed.
#
# Usage: tools/syntax-check.sh

set -uo pipefail

UNITY_DATA="/c/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data"
PROJECT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${TMPDIR:-/tmp}/wizardwars-syntaxcheck.dll"

if [ ! -f "$UNITY_DATA/DotNetSdkRoslyn/csc.dll" ]; then
  echo "Unity's Roslyn not found at $UNITY_DATA/DotNetSdkRoslyn/csc.dll" >&2
  exit 127
fi

REFS=""
for dll in "$UNITY_DATA/Managed/UnityEngine/"*.dll; do
  REFS="$REFS -r:\"$(cygpath -w "$dll")\""
done
REFS="$REFS -r:\"$(cygpath -w "$UNITY_DATA/NetStandard/ref/2.1.0/netstandard.dll")\""

SRCS=$(find "$PROJECT/Assets/Scripts" -name "*.cs" | while read -r f; do
  printf '"%s" ' "$(cygpath -w "$f")"
done)

echo "Compiling $(find "$PROJECT/Assets/Scripts" -name '*.cs' | wc -l) scripts with Unity's Roslyn..."
output=$(eval dotnet \"$UNITY_DATA/DotNetSdkRoslyn/csc.dll\" -nologo -target:library \
  -nostdlib+ -langversion:9 -out:\"$(cygpath -w "$OUT")\" $REFS $SRCS 2>&1)
status=$?

if [ -n "$output" ]; then
  echo "$output"
fi

errors=$(printf '%s' "$output" | grep -c "error CS")
warnings=$(printf '%s' "$output" | grep -c "warning CS")
echo "C# errors: ${errors}   C# warnings: ${warnings}"

if [ "$status" -ne 0 ] || [ "$errors" -gt 0 ]; then
  exit 1
fi
echo "Syntax check passed."
