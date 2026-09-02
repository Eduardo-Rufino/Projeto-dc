# AGENTS.md — Projeto DC

Jogo fã-feito de Digimon (inspirado em Digimon World Championship). Godot 4.7 + C# (.NET 8), Forward Plus, Jolt Physics 3D, D3D12.

Este arquivo é a referência canônica de arquitetura/estado do projeto, usada por qualquer agente de IA (Claude Code, Copilot, etc). Instruções específicas de uma ferramenta ficam nos arquivos próprios dela (ex.: `CLAUDE.md`, `.github/copilot-instructions.md`), que referenciam este documento em vez de duplicar conteúdo.

## Stack & tooling

- **Engine**: Godot 4.7, C# via `Godot.NET.Sdk/4.7.0`, target `net8.0` (`net9.0` para build Android)
- **RootNamespace**: `ProjetoDC` (definido no `.csproj`)
- **Sem testes automatizados**, sem linter/formatter configurado além de `.editorconfig` (utf-8)
- **Ignorados no git**: `.godot/`, `.vs/`, `.mono/`, `data_*/`, `mono_crash.*.json`
- **Novo arquivo JSON em `Data/`**: precisa ser adicionado manualmente ao `.csproj` (`<Content Include="Data\...\arquivo.json" />`) ou não entra no build

## Diretrizes de gameplay

- **Não altere a lógica de gameplay** (batalha e treino) sem confirmar com o desenvolvedor; preferir mudanças que mantenham o comportamento existente e evitar gambiarras.
- Ao ajustar lógica de jogo, usar as instâncias do `Center` para batalhas (não criar instâncias separadas), oferecer overloads para definir Digimon por instância, e evitar conexões de evento duplicadas.

## Arquitetura

- **`Main.cs`** (raiz, sem namespace) — entry point: garante `GameManager`/`DatabaseManager` prontos, carrega Digimons/Evoluções via `DatabaseManager`, abre `Scenes/Center/CenterScreen.tscn`
- **Autoloads (singletons)**:
  - `GameManager` (`Scripts/Managers/GameManager.cs`) — dono de `SaveData` (`Save`), `CenterService`, `TrainingSystem`, `EggSystem`, `EnemyGenerator`, `ClockSystem`, `SaveSystem`, `BattleSystem`, e dos digimons `PlayerDigimon`/`EnemyDigimon`. Ponto central de quase toda ação de gameplay (treinar, alimentar, comprar, curar, batalhar). Salva o jogo em `_Notification(NotificationWMCloseRequest)` e ao final de cada dia.
  - `DatabaseManager` — carrega dados estáticos JSON de `Data/Digimon/*.json` e `Data/Evolutions/*.json` no startup
- **Persistência** (`Scripts/Save/`, `Scripts/Systems/Save/`):
  - `SaveData` — raiz do save: `WorldState` (relógio/dia) + `CenterState`
  - `SaveSystem` — serializa `SaveData` como JSON em `user://save.json` via `System.Text.Json`
- **Dados estáticos** (`Scripts/Data/`): `DigimonData` (template: Id, Code, Name, Stage, Role, Attribute, Element, EggType, AttackType, BaseStats), `BaseStats`, `EvolutionData`, `EggData`
- **Runtime** (`Scripts/Gameplay/`):
  - `DigimonInstance` — instância mutável: Level/XP/HP/CurrentStats (cópia do BaseStats do template), Hunger/MaxHunger, Stamina, `HealthState` (Healthy/Sick, evento `HealthStateChanged`), `Activity` (Idle/Sleeping/Training/Eating, evento `ActivityChanged`), `AgeInDays`, `CapacityCost` (derivado do stage). `AdvanceHour`/`AdvanceDay` avançam fome/idade/sono/chance de doença; `GainExperience` dispara level-up e evolução.
  - `Food`, `DigimonWorld`
- **Sistemas** (`Scripts/Systems/`):
  - `Battle/BattleSystem` — turnos, velocidade decide ordem, dano = ATK − DEF (mín. 1), ±10% variância, 10% crit (2x); `BattleSimulator` é legado não usado
  - `Evolution/EvolutionSystem` (estático) — checa nível/idade, aplica maior prioridade válida e `StatMultiplier`
  - `Training/TrainingSystem`, `Eggs/EggSystem`, `Center/ClockSystem` (dispara `MinutePassed`/`HourPassed`/`DayPassed`), `Center/CenterService` (CRUD sobre `CenterState`), `Save/SaveSystem`
- **Mundo/Centro** (`Scripts/Models/World/`):
  - `WorldState` — dia/hora/minuto atual
  - `CenterState` — Bits (moeda), Meat, Medicine, CapacityLimit/CapacityUsed (soma de `CapacityCost`), lista de `Digimons`/`Eggs`/`Foods`
  - `CenterGrid`, `CenterArea` (área hexagonal com `CenterAreaType`: Neutral/Training/Dormitory/Restaurant/Hospital; usa `Polygon2D` para bounds e ponto aleatório interno), `CenterExpansionSlot`/`CenterExpansionSlotVisual`/`CenterAreaVisual` — sistema de grid hexagonal e expansão de áreas do centro
  - `FoodWorld`, `MedicineWorld` — áreas visuais especializadas dentro do centro
- **UI** (`Scripts/UI/`): `CenterScreen`, `TrainingScreen`, `BattleScreen`, `BattleResultScreen`, `EnemySelectionScreen`, `ShopScreen`, `DigimonSprite`, `AreaBuildMenu` (menu de construção de área), `HUD`

## Estado do projeto

- Save/Load, loja (Bits/Meat/Medicine), relógio de jogo, fome/doença, batalha por turnos, evolução, grid hexagonal de áreas do centro e visual de digimons já implementados
- Muitas pastas ainda são scaffolding vazio: `Data/`: Buildings, Eggs, Itens, Maps, NPCs, Passives, Roles, Tournaments, Training; `Scenes/`: Digimon, Exploration, Tournament, Training (parcial), UI; `Scripts/Core` (parcial), `Scripts/Utils`
- Trabalho recente/ativo: expansão de áreas do centro (`AreaBuildMenu`, `MedicineWorld`), nova `HUD`

## Gotchas

- `GameManager`/`DatabaseManager` usam `CallDeferred` para inicialização cruzada — não assuma que singletons estão prontos em `_Ready()` de outros autoloads
- JSON usa `PropertyNameCaseInsensitive` + `JsonStringEnumConverter` — valores de enum devem bater exatamente com o nome do enum C# (ex.: `"Rookie"`, não `"rookie"`)
- `DigimonInstance.CurrentStats` é uma **cópia** de `BaseData.BaseStats` (via `CloneBaseData`) — editar `CurrentStats` não altera o template
- Classes em `Scripts/Data/` têm setters públicos (POCOs mutáveis para deserialização)
- Ações de gameplay (alimentar, curar, comprar, treinar, evoluir) ficam centralizadas em `GameManager`, não espalhadas pelas telas de UI
- `CenterState.Bits` começa em 0 (save novo começa zerado, sem Bits/itens de debug) e `CapacityLimit` em 10 — `CapacityLimit` ainda é placeholder, não balanceamento final

## Namespaces

- `ProjetoDC.Enums` — todos os enums (pasta raiz `Enums/`, não `Scripts/Enums`)
- `ProjetoDC.Scripts.Data` — POCOs de dados estáticos
- `ProjetoDC.Scripts.Gameplay` — instâncias em runtime
- `ProjetoDC.Scripts.Managers` — singletons autoload
- `ProjetoDC.Scripts.Models.World` — estado e visuais do mundo/centro
- `ProjetoDC.Scripts.Systems.*` — batalha, evolução, treino, ovos, relógio, save
- `ProjetoDC.Scripts.Save` — `SaveData`
- `ProjetoDC.Scripts.UI` — scripts de tela
