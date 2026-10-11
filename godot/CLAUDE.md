# CLAUDE.md — Wizard Wars (Godot port)

Working notes for agents in `godot/`. The plan, phases and design decisions live in
`../docs/GODOT_PORT_PLAN.md`; read it before starting a phase. The Unity project in the
repo root is the **reference** for behaviour and tuning until parity sign-off. Read its
code and `../CLAUDE.md` to learn what a feature does, and do not change Unity gameplay
while the port runs.

## Status

Phase 0 (scaffolding) is done. The project builds and passes the headless checks. There
is no gameplay yet; `Main.tscn` shows a placeholder label.

## Stack

- Godot **4.5.1 .NET** (mono), C#: `Godot.NET.Sdk/4.5.1`, `net9.0`, `Nullable=annotations`.
- GL Compatibility renderer, 1920×1080, stretch `canvas_items` / `expand`, 2D gravity 0.
- Godot binary: `$GODOT_BIN`, or by default
  `C:/Program Files (x86)/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64_console.exe`.
  Use the **console** exe so output reaches the terminal.

## Verify every change

```bash
bash tools/check.sh
```

1. `dotnet build` — C# compiles.
2. Headless `--import` — assets import and `.uid` files are generated.
3. A headless run of `Main.tscn -- --checks` — `RegressionChecks` prints
   `RegressionChecks: PASS|FAIL …` lines and the exit code is the failure count.

`dotnet build` alone is **not** verification: it cannot see scene wiring, resource paths
or signals. Gameplay still needs an Editor playtest at each phase gate.

When a rule is easy to break silently (a tuning invariant, a resource path, a physics
layer, friendly fire), add a check to `scripts/RegressionChecks.cs`.

## Known Godot gotchas (from DrawFight and Wizard Survivors)

- "Unrecognized UID" or "Failed loading resource" before the first import means the asset
  has not been imported yet, not that something is broken. Run `tools/check.sh` (it
  imports) after adding any art.
- A new PNG is `null` from `GD.Load` until it has been imported, and that fails silently.
- `.uid` and `.import` sidecar files are committed. `.godot/`, `bin/`, `obj/` and
  `build/` are not. Never read or edit `.godot/`.
- Calling `Call("Method", …)` from C# must pass every argument, including ones with C#
  defaults, or it fails silently.
- Use `ResourceLoader.Load` with the default cache mode. `CacheMode.Replace` returned blank
  instances on first load in Wizard Survivors.
- Menus built in code must grab focus, or a gamepad cannot use them.
- Collision bugs are usually mask mismatches. Use the named layers below.
- C# Godot cannot export to the web. The target is Windows desktop.
- Exporting needs the exact mono export templates in
  `%APPDATA%\Godot\export_templates\4.5.1.stable.mono\`.

## Conventions

- Flat `scripts/` and `scenes/`; data resources in `data/`; art, sound and music in `assets/`.
  Debug and probe scenes are prefixed `_`.
- `public partial class` for nodes, `[Export]` auto-properties, tabs, and comments that
  explain *why*.
- **Units:** every Unity distance, radius, speed or position goes through `Units.U(...)`
  (90 px per Unity unit; Unity +y is Godot up). Tuning values can then be copied
  straight from the Unity docs.
- **Physics layers** (named in `project.godot`, checked by `RegressionChecks`):
  1 `solids`, 2 `wizards`, 3 `projectiles`, 4 `zones`.
- **Input:** poll devices directly through `IInputSource` (the DrawFight pattern), not the
  InputMap. The AI is an `IInputSource` too.
- **Tuning** lives only in `.tres` data resources, never duplicated in scenes.
- One source of truth for match state (`Session`). Do not recreate Unity's
  GlobalHandler/bridge split.
- Every hostile effect goes through `Combat.Apply(hit, target)`, which owns the
  friendly-fire rule. Status effects are keyed by their source, so they cannot leak.
