---
name: run-projeto-dc
description: Build, run, screenshot, and check the logs of Projeto DC (Godot 4.7 C#/.NET 8 desktop game). Use when asked to run the game, start it, launch it, build it, take a screenshot of it, verify a change works in the real app, or check its logs for errors after a change.
---

Projeto DC is a Windows-only Godot 4.7 (C#/.NET 8, Mono build) desktop game — there is no headless "just run the logic" mode with rendering, and no browser/Electron surface to drive with a web tool. The agent handle is `.claude/skills/run-projeto-dc/driver.ps1`, a PowerShell script with five actions (`build`, `import`, `launch`, `screenshot`, `check`, `stop`) — run it with `powershell -File`. All paths below are relative to the project root (`C:\Users\barce\OneDrive\DOCUMENTOS\PROJETO-DC`), which the driver resolves on its own from its own location.

There is no click-driven UI automation here (no Playwright-for-Godot equivalent available) — the driver's ceiling is launch, screenshot, and log inspection. That's still enough to catch startup crashes, missing/unimported assets, and broken scene references, and to visually confirm a change rendered. To reach an internal screen (encyclopedia, inventory, team selection, etc.) for a screenshot without clicking anything, set the `DEBUG_OPEN_SCREEN` environment variable before launching — see "Opening an internal screen for a screenshot" below.

## Prerequisites

- **.NET 8 SDK** — `dotnet build` must resolve on PATH (already true in this environment).
- **Godot 4.7 Mono build**, console variant, found at:
  `C:\Users\barce\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe`
  Use the `_console.exe` one, not the plain `.exe` — only the console variant attaches a console so stdout/stderr are actually capturable. If this path doesn't exist (different machine), find it with `Get-ChildItem -Recurse -Filter "Godot_v4.7*console.exe" ~\Downloads` and update the `$GodotExe` line at the top of `driver.ps1`.
- No package installs needed beyond the above — everything else (project.godot, autoloads) is already configured.

## Build

```powershell
powershell -File .claude\skills\run-projeto-dc\driver.ps1 -Action build
```

Wraps `dotnet build "Projeto DC.csproj" -v quiet`. Expect `0 Erro(s)`. Warnings are pre-existing (nullable-annotation and unawaited-Task warnings in a few files) — don't chase those unless they're new.

## Run (agent path)

```powershell
# 1. If you added/changed any asset under Assets/ or a .tscn (new Digimon sprites, new
#    scenes, etc.), import it first - see Gotchas, this is not optional.
powershell -File .claude\skills\run-projeto-dc\driver.ps1 -Action import

# 2. Launch the actual game (opens a real window on the screen - this is expected).
powershell -File .claude\skills\run-projeto-dc\driver.ps1 -Action launch
# -> "Launched PID <n>, log at C:\Users\barce\AppData\Local\Temp\claude\projeto_dc_run.log"

# 3. Give it a few seconds to boot and load the save, then check the log for problems.
powershell -File .claude\skills\run-projeto-dc\driver.ps1 -Action check
# -> "Clean - no ERROR/Exception/Falha/'Frame não encontrado' lines in <log>"
#    or prints the first 30 matching lines and exits 1.

# 4. Take a screenshot to visually confirm what rendered (look at it - a blank/black
#    window is a failure to launch even if `check` came back clean).
powershell -File .claude\skills\run-projeto-dc\driver.ps1 -Action screenshot -OutPath C:\path\to\out.png

# 5. Always stop it when done - it's a real window that otherwise keeps running.
powershell -File .claude\skills\run-projeto-dc\driver.ps1 -Action stop
```

| action | what it does |
|---|---|
| `build` | `dotnet build` the csproj, exits non-zero on compile errors |
| `import` | Headless Godot editor pass (`--headless --editor --quit-after 2000`) that imports any new/changed assets; exits on its own |
| `launch` | Starts the real game window (`--path . --verbose`), logs to `-LogPath` (default `%TEMP%\claude\projeto_dc_run.log`), returns immediately with the PID |
| `check` | Greps the launch log for `ERROR|Exception|Falha|Frame não encontrado`; exit 0 if clean, exit 1 + first 30 hits if not |
| `screenshot` | Finds the running game window by title (`*Projeto DC*`) and captures just that window to `-OutPath`; falls back to full-screen capture if the window isn't found (e.g. not finished booting yet) |
| `stop` | Kills both the console launcher and the actual game process |

Default artifact locations if you don't pass `-LogPath`/`-OutPath`: log at `%TEMP%\claude\projeto_dc_run.log`, screenshot at `%TEMP%\claude\projeto_dc_screenshot.png`.

A `launch` takes about 5-8 seconds before the window is up and the save is loaded — don't screenshot or `check` immediately after `launch` returns; wait first (e.g. `Start-Sleep -Seconds 8` or equivalent).

### Opening an internal screen for a screenshot

`Center.cs` (`Scenes/Center/Center.cs`, `DebugOpenScreen`) reads the `DEBUG_OPEN_SCREEN` env var once at boot and opens the matching screen automatically — the only way to get a screenshot of something other than the base Center view without click automation. No effect if unset; safe to leave code in place. Set it in the *same* PowerShell session before calling `Start-Process` (the driver's `launch` action doesn't expose this itself — call `Start-Process` directly, same args the driver uses, see its `launch` block for the exact command):

```powershell
$env:DEBUG_OPEN_SCREEN = "encyclopedia"   # or: inventory, team, evolutioncapacity, explorationpick
# ...then Start-Process the game the same way -Action launch does...
```

Known values (add more in `Center.DebugOpenScreen` as needed — each case just needs *some* valid sample data, doesn't need to represent real game state): `encyclopedia`, `inventory`, `team`, `evolutioncapacity`, `explorationpick`.

## Run (human path)

Double-click the console `.exe` above, or in the Godot editor GUI hit Run (F5). Ctrl-C in the console window (or just close it) to stop. Not meaningfully different from the agent path other than not being scriptable.

## Test

No automated test suite exists for this project (see AGENTS.md) — `build` + `launch` + `check` + a screenshot IS the test.

---

## Gotchas

- **New assets are invisible until imported — and this fails silently, not loudly.** A newly-added PNG or `.tscn` with no matching `.import`/import-cache entry makes `GD.Load<Texture2D>(...)` fail, which `DigimonSprite.cs` (and similar loaders) catch and downgrade to `GD.PrintErr("Frame não encontrado: ...")` — the game keeps running with an invisible sprite, it does not crash or throw. Always run the `import` action after adding assets, before trusting a `launch`+`check` as green. Watch the `import` output for `Started (Re)Importando Recursos (N steps)` ... `[ DONE ] reimport` — if you pass a `-QuitAfter` too low for a big batch (e.g. 60-120 frames for hundreds of pending files), it quits mid-import having done nothing useful. 2000 was enough for 736 pending files; scale up for bigger batches.
- **The game window and the console window are two separate processes with two separate titles.** Launching `..._console.exe` spawns *both* a console window titled "Godot Engine (Console)" (empty, just relays stdout — this is NOT the game) *and* a second process (plain `Godot_v4.7-stable_mono_win64`, no `_console` suffix) whose window is titled `Projeto DC (DEBUG)` in a debug run — that's the one worth screenshotting. `driver.ps1 -Action screenshot` already targets the right one via `Get-Process -Name "Godot_v4.7-stable_mono_win64" | Where MainWindowTitle -like "*Projeto DC*"`; don't try to `FindWindow` on an exact "Projeto DC" title, it won't match the "(DEBUG)" suffix.
- **`PrintWindow` can silently drop content right at the window's bottom edge on this D3D12/Forward+ renderer.** Hit this for real: a row of buttons positioned with their bottom ~0-30px from the window's bottom edge was completely invisible in the `screenshot` action's output (not dim, not clipped-by-a-few-pixels — just absent), while a `GetWindowRect`+`SetForegroundWindow`+`CopyFromScreen` capture of the exact same frame showed them fine, confirming the game was rendering correctly and only the capture was wrong. Don't trust a `screenshot` showing content missing/cut off right at the bottom edge as proof of a real layout bug — if something looks absent only in that bottom strip, cross-check with an actual full-screen region capture (bring the window to front, `CopyFromScreen` over its `GetWindowRect`) before concluding it's not rendering. This wasted real debugging time once already (added a temp bright-red `modulate` to prove the buttons existed, which is a reasonable fallback trick if you hit this again — but check with a screen-capture cross-check first).
- **The `screenshot` action uses `PrintWindow` (captures the window's own composited content), not `SetForegroundWindow`+`CopyFromScreen` (captures a region of the visible screen).** An earlier version used the latter — `SetForegroundWindow` is unreliable when called from a background/unfocused process (a documented Win32 restriction) and silently no-ops instead of erroring, so `CopyFromScreen` ended up capturing whatever *other* window already had focus/was on top (caught this when a screenshot came back showing an unrelated 3D game instead of Projeto DC, with no error anywhere). `PrintWindow` with `PW_RENDERFULLCONTENT` (flag `2`) captures correctly even when the game window is occluded, minimized-to-taskbar-but-not-minimized, or not focused — don't regress this back to a screen-region capture.
- **PowerShell `New-Object Typename(args...)` with an expression inside the parens** (e.g. `New-Object Bitmap($w * 2, $h)`) silently mis-parses as a single array argument and throws `op_Multiply`/`op_Subtraction not found` deep in a .NET method, not a parse error — compute values into plain variables first, then `New-Object Typename $a, $b` with no parens around the arg list.
- **Don't confuse this project's own `System.Drawing`-based scratch scripts** (used elsewhere for slicing/upscaling sprite sheets) **with this driver** — they're unrelated one-off tools, not part of running the app.
- **A brand-new `.tscn`/`.cs` pair (a new screen, not just new assets in an existing one) needs the same `import` pass as new PNGs — but the failure mode is worse: `ERROR: Unrecognized UID: "uid://..."` at the very start of boot, for autoloads (`GameManager`, `DatabaseManager`) that have nothing to do with the new files.** `project.godot`'s `[autoload]` section references those scripts by `uid://`, not by path; adding a new scene that references other new files (e.g. a new screen wired into `Center.tscn`) can leave the engine's UID cache transiently out of sync, and a `launch` right after editing will throw those errors before any of your own code runs. A single `import` run fixes it, but **only if it actually reaches its own `[ DONE ]`, not just the `first_scan_filesystem` step's `[ DONE ]`** — a `-QuitAfter` that's plenty for a PNG batch (500-1000) can still be too low here and get killed mid-cache-rebuild with `WARNING: Scan thread aborted...`, silently leaving the UID errors in place. If a `launch` right after adding a new scene/script shows `Unrecognized UID` for files you didn't touch, don't chase them as a real bug — just re-run `import` with a larger `-QuitAfter` (2000 was enough) and confirm the *reimport* phase's own `[ DONE ]` printed, then `launch` again.

## Troubleshooting

- **`check` reports `Frame não encontrado: res://Assets/Sprites/Digimon/<x>/N.png`**: the asset was never imported. Run `-Action import` (with a high enough `-QuitAfter`, see Gotchas), then `launch` again.
- **`screenshot` prints "Game window not found ... falling back to full-screen capture"**: either the game hasn't finished booting yet (wait longer after `launch`) or it crashed on startup — check `check` first, or read the raw log file at `-LogPath`.
- **Editor headless import prints `ERROR: Cannot open file 'res://Scripts/UI/<X>.cs'` / `Failed to read file` / `Failed loading resource`**: a `.tscn` under `Scenes/` references a `.cs` script that no longer exists on disk (found this for two dead scenes from the old 1v1 battle system, since removed). Find the culprit scene with:
  `Get-ChildItem -Recurse Scenes -Filter *.tscn | Select-String 'path="res://.*\.cs"' | % { if (-not (Test-Path ($_.Matches.Groups[0].Value -replace 'path="res://','' -replace '"',''))) { $_.Path } }`
  then either restore the missing script or delete the orphaned scene if nothing else references it (`Select-String -Path Scenes\**\*.tscn,Scripts\**\*.cs -Pattern '<SceneName>\.tscn'` to confirm before deleting).
