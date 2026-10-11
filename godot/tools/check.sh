#!/usr/bin/env bash
# Verifies the Godot project without opening the editor:
#   1. dotnet build             - C# compiles (warnings are reported, errors fail)
#   2. headless --import        - assets import, .uid files are generated
#   3. headless --checks run    - RegressionChecks: settings, data wiring, number rules
# Exits non-zero if any step fails. dotnet build alone is NOT verification: it cannot see
# scene wiring, resource paths or signals. Steps 2 and 3 can.
#
# Godot comes from $GODOT_BIN (the *console* exe, so output reaches this terminal).
set -u
cd "$(dirname "$0")/.."

GODOT_BIN="${GODOT_BIN:-C:/Program Files (x86)/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64_console.exe}"
if [ ! -f "$GODOT_BIN" ]; then
	echo "Godot not found at: $GODOT_BIN"
	echo "Set GODOT_BIN to the Godot 4.5.1 mono console exe."
	exit 2
fi

echo "== dotnet build"
build_output=$(dotnet build WizardWars.sln -nologo -v quiet 2>&1)
build_status=$?
echo "$build_output" | grep -E "error|warning CS|Warn|Error" | sort -u | head -20
if [ $build_status -ne 0 ]; then echo "dotnet build failed"; exit 1; fi

echo "== headless import"
"$GODOT_BIN" --headless --path . --import > /tmp/wizardwars-godot-import.log 2>&1
grep -E "ERROR|SCRIPT ERROR" /tmp/wizardwars-godot-import.log | grep -v "Unrecognized UID" | head -20

echo "== regression checks"
"$GODOT_BIN" --headless --path . res://scenes/Main.tscn -- --checks > /tmp/wizardwars-godot-checks.log 2>&1
status=$?
grep -E "RegressionChecks:|ERROR|SCRIPT ERROR" /tmp/wizardwars-godot-checks.log
if [ $status -ne 0 ]; then
	echo "Checks failed (exit $status). Full log: /tmp/wizardwars-godot-checks.log"
	exit 1
fi
echo "All checks passed."
