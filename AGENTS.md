# AGENTS.md — Projeto DC

Fan-made Digimon game (inspired by Digimon World Championship). Godot 4.7 + C# (.NET 8), Forward Plus renderer, Jolt Physics 3D, D3D12.

## Stack & tooling

- **Engine**: Godot 4.7, C# via `Godot.NET.Sdk/4.7.0`, target `net8.0`
- **Namespace root**: `ProjetoDC` (csproj sets `RootNamespace`)
- **No tests**, no linter, no formatter config beyond `.editorconfig` (utf-8 only)
- **Ignore list** (`.gitignore`): `.godot/`, `.vs/`, `.mono/`, `data_*/`, `mono_crash.*.json`

## Architecture

- **`Main.cs`** — entry point, loads DB (`CallDeferred`), then opens `Scenes/TrainingCenter.tscn`
- **Singletons (autoload)**:
  - `GameManager` — owns `WorldState`, `CenterState`, `CenterService`, player/enemy `DigimonInstance`, `BattleSystem`. All state accessed via `GameManager.Instance`
  - `DatabaseManager` — loads JSON data from `Data/Digimon/*.json` and `Data/Evolutions/*.json` at startup
- **Scene tree**: TrainingCenter (`Control`) is the main UI — training buttons, battle, day advance, digimon selectors
- **Data model** (`Scripts/Data/`):
  - `DigimonData` — static template (Id, Code, Name, Stage, Role, Attribute, Element, EggType, BaseStats)
  - `BaseStats` — HP, PhysAtk/Def, SpAtk/Def, Speed
  - `EvolutionData` — FromId→ToId, RequiredLevel, RequiredAgeInDays, Priority, StatMultiplier
- **Runtime model** (`Scripts/Gameplay/`):
  - `DigimonInstance` — wraps `DigimonData` + mutable Level, HP, XP, Age, CurrentStats, `CapacityCost` (derived from stage via `GetCapacityCostForStage`). Train methods modify stats directly. `GainExperience` triggers `EvolutionSystem.TryToEvolve`
- **Systems**:
  - `EvolutionSystem` (internal static) — checks level/age requirements, picks highest-priority valid evolution, applies `StatMultiplier`
  - `BattleSystem` — turn-based, speed determines order, damage = ATK - DEF (min 1), ±10% variance, 10% crit (2x)
  - `BattleSimulator` (internal static) — unused legacy, uses base stats directly instead of current stats
- **World** (`Scripts/Models/World/`):
  - `WorldState` — `CurrentDay` counter
  - `CenterState` — `Bits` (currency), capacity limit/used, digimon roster
  - `CenterService` — thin wrapper over CenterState CRUD

## Project state

- **Only 4 digimon defined** (Agumon, Gabumon, Patamon, Tentomon in `Data/Digimon/`)
- **Only 1 evolution chain** (Agumon → id:2 at lv5, → id:3 at lv10 in `Data/Evolutions/agumon_evolutions.json`)
- **Many directories are empty scaffolding** (no content yet):
  - `Data/`: Buildings, Eggs, Itens, Maps, NPCs, Passives, Roles, Tournaments, Training
  - `Scenes/`: Battle, Common, Digimon, Exploration, Main, Tournament, Training, UI
  - `Scripts/`: Core, Utils
- **No save/load system implemented** yet

## Key gotchas

- GameManager and DatabaseManager use `CallDeferred` for cross-initialization — don't assume singletons are ready in `_Ready()` of autoloaded nodes
- JSON deserialization uses `PropertyNameCaseInsensitive` + `JsonStringEnumConverter` — enum values must match C# enum names exactly (e.g., `"Rookie"` not `"rookie"`)
- `DigimonInstance.CurrentStats` starts as a **copy** of `BaseData.BaseStats` — editing CurrentStats does NOT modify the template data
- All `Scripts/Data/` classes use `public` setters (mutable POCOs for JSON deserialization)
- `TrainingCenter.cs` uses `GetNodeOrNull` for all UI references — node paths are defined in `InitializeUIComponents()`
- Bits/Capacity in `RefreshUI` access CenterState directly via `Game.Center`
- C# project items (`Content` in csproj) explicitly list every JSON data file — new data files must be added there or they won't be included in builds

## Namespace conventions

- `ProjetoDC.Enums` — all enums (BattleResult, DigimonStage, RoleType, EggType, etc.)
- `ProjetoDC.Scripts.Data` — data POCOs
- `ProjetoDC.Scripts.Gameplay` — runtime instances
- `ProjetoDC.Scripts.Managers` — autoload singletons
- `ProjetoDC.Scripts.Models.World` — world/center state models
- `ProjetoDC.Scripts.Systems` — battle, evolution, center systems
- `ProjetoDC.Scripts.UI` — scene scripts
