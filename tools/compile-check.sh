#!/usr/bin/env bash
# Compile-check the Wizard Wars project without opening the Unity Editor.
#
# Runs Unity in batch mode, which imports assets and compiles Assembly-CSharp.
# Exits non-zero if Unity fails or the log contains C# errors.
#
# The Unity Editor must be CLOSED: batch mode needs an exclusive lock on Library/.
#
# Usage: tools/compile-check.sh [path/to/logfile]

set -uo pipefail

UNITY_VERSION="2022.3.62f3"
UNITY_EXE="/c/Program Files/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity.exe"
PROJECT_PATH="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG="${1:-${TMPDIR:-/tmp}/wizardwars-compile.log}"

if [ ! -x "$UNITY_EXE" ]; then
  echo "Unity ${UNITY_VERSION} not found at: $UNITY_EXE" >&2
  exit 127
fi

if tasklist 2>/dev/null | grep -qi '^Unity\.exe'; then
  echo "Unity Editor appears to be running. Close it first; batch mode needs Library/." >&2
  exit 1
fi

rm -f "$LOG"
echo "Compiling ${PROJECT_PATH} with Unity ${UNITY_VERSION}..."
"$UNITY_EXE" -batchmode -quit -nographics \
  -projectPath "$(cygpath -w "$PROJECT_PATH" 2>/dev/null || echo "$PROJECT_PATH")" \
  -logFile "$(cygpath -w "$LOG" 2>/dev/null || echo "$LOG")"
unity_status=$?

errors=0
if [ -f "$LOG" ]; then
  errors=$(grep -c "error CS" "$LOG")
  warnings=$(grep -c "warning CS" "$LOG")
  echo "C# errors: ${errors}   C# warnings: ${warnings}"
  if [ "$errors" -gt 0 ]; then
    echo "--- first 20 errors ---"
    grep -m20 "error CS" "$LOG"
  fi
  grep -i -m10 "Could not extract GUID\|Compilation failed" "$LOG"
else
  echo "No log written at $LOG" >&2
fi

echo "Unity exit code: ${unity_status}   Log: ${LOG}"
if [ "$unity_status" -ne 0 ] || [ "$errors" -gt 0 ]; then
  exit 1
fi
echo "Compile check passed."
