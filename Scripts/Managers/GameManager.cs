using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Core.Results;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Save;
using ProjetoDC.Scripts.Systems.Battle;
using ProjetoDC.Scripts.Systems.Center;
using ProjetoDC.Scripts.Systems.Clock;
using ProjetoDC.Scripts.Systems.Eggs;
using ProjetoDC.Scripts.Systems.Evolution;
using ProjetoDC.Scripts.Systems.Jogress;
using ProjetoDC.Scripts.Systems.Passives;
using ProjetoDC.Scripts.Systems.Results;
using ProjetoDC.Scripts.Systems.Save;
using ProjetoDC.Scripts.Systems.Training;
using ProjetoDC.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Managers
{
    /// <summary>
    /// Gerencia o estado geral do jogo: salvar, inicializar sistemas (centro, treinos, ovos),
    /// selecionar digimons para batalha e controlar fluxos relacionados ao tempo.
    /// </summary>
    public partial class GameManager : Node
    {
        public static GameManager Instance { get; private set; }

        /// <summary>Digimon do jogador atualmente selecionado (fora de batalha: treino, alimentação, etc.).</summary>
        public DigimonInstance PlayerDigimon { get; private set; }

        /// <summary>Times da batalha 3x3 em andamento.</summary>
        public List<DigimonInstance> PlayerBattleTeam { get; private set; } = new();
        public List<DigimonInstance> EnemyBattleTeam { get; private set; } = new();

        public event Action PlayerDigimonChanged;
        public event Action GameLoaded;
        public event Action<BattleResult> TeamBattleFinished;
        public event Action TeamBattleStarted;

        /// <summary>True enquanto a batalha em andamento foi disparada de dentro de uma área
        /// de exploração (ver StartWildEncounter), não da tela de batalha do Center - Center e
        /// ExplorationArea escutam TeamBattleStarted/TeamBattleFinished e usam isso pra saber
        /// qual dos dois deve reagir (esconder/mostrar), já que só um dos dois pode ter sido
        /// a origem. Só é confiável durante a própria invocação de TeamBattleStarted/Finished -
        /// é resetado logo depois (ver OnTeamBattleFinished).</summary>
        public bool BattleFromExploration { get; private set; }

        private int? _activeWildEncounterDigimonId;

        private static readonly Random _random = new();

        /// <summary>Disparado quando um Digimon atende os requisitos de evolução mas o
        /// Center não tem capacidade livre pra nova forma - carrega o Digimon bloqueado, a
        /// forma que ele evoluiria e quanto falta de capacidade. Não dispara de novo pro
        /// mesmo Digimon enquanto ele não escolher "manter sem evoluir"
        /// (DigimonInstance.EvolutionCapacityWarningDismissed) - ver TryToEvolve.</summary>
        public event Action<DigimonInstance, DigimonData, int> EvolutionBlockedByCapacity;

        /// <summary>Disparado quando um Digimon é removido permanentemente do Center (ver
        /// DeleteDigimon) - quem desenha o Center escuta isso pra tirar o DigimonWorld
        /// visual correspondente, senão ele continuaria andando por aí sem existir mais
        /// no save.</summary>
        public event Action<DigimonInstance> DigimonDeleted;

        /// <summary>Disparado quando uma fusão Jogress acontece de verdade (ver TryToFuse),
        /// com os dois Digimons de origem (já removidos do Center) e o Digimon fundido (já
        /// adicionado) - quem desenha o Center escuta isso pra tirar os dois DigimonWorld
        /// visuais antigos e criar o novo (mesmo espírito de EggSystem.EggHatched).</summary>
        public event Action<DigimonInstance, DigimonInstance, DigimonInstance> JogressFused;

        /// <summary>Disparado quando um Digimon morre de verdade (velhice ou maus-tratos -
        /// ver KillDigimon), com o nome já formatado e o motivo em texto pronto pra mostrar
        /// ao jogador. Sempre acompanhado de um DigimonDeleted (a morte também é uma remoção
        /// permanente do Center) - quem só precisa tirar o visual escuta DigimonDeleted, quem
        /// precisa avisar o jogador escuta este aqui.</summary>
        public event Action<DigimonInstance, string> DigimonDied;

        /// <summary>Estado puro da batalha 3x3 ativa (nulo fora de batalha).</summary>
        public BattleMatch BattleMatch { get; private set; }

        /// <summary>True do início de LaunchBattle até a arena avisar que terminou - StartX
        /// (batalha livre, campeonato, encontro selvagem) recusam iniciar uma nova batalha
        /// enquanto isso for true. Existe porque um clique perdido consegue vazar pra um
        /// elemento clicável escondido atrás da arena de batalha (ex.: outro selvagem na área
        /// de exploração, que continua processando input mesmo invisível) e disparar uma
        /// segunda LaunchBattle por cima da primeira - duas BattleMatch/BattleArena vivas ao
        /// mesmo tempo corrompem BattleFromExploration e o time em PlayerBattleTeam/
        /// EnemyBattleTeam (compartilhados, não por batalha), deixando o jogo preso mostrando
        /// pedaços da exploração e do Center juntos.</summary>
        public bool IsBattleActive => BattleMatch != null;

        /// <summary>Motivos independentes que podem estar pedindo pra pausar a árvore ao
        /// mesmo tempo (batalha, animação de evolução, tela secundária aberta na HUD, etc.) -
        /// GetTree().Paused só fica true enquanto pelo menos um motivo estiver ativo, e só
        /// volta a false quando TODOS forem liberados. Sem isso, cada sistema mexendo direto
        /// em GetTree().Paused derrubava a pausa dos outros (ex.: fechar/abrir uma tela da
        /// HUD despausando uma batalha ou animação de evolução em andamento).</summary>
        private readonly HashSet<string> _pauseReasons = new();

        public void RequestPause(string reason)
        {
            _pauseReasons.Add(reason);

            GetTree().Paused = _pauseReasons.Count > 0;
        }

        public void ReleasePause(string reason)
        {
            _pauseReasons.Remove(reason);

            GetTree().Paused = _pauseReasons.Count > 0;
        }

        public SaveData Save { get; private set; }
        public CenterService CenterService { get; private set; }
        public TrainingSystem TrainingSystem { get; private set; }
        public EggSystem EggSystem { get; set; }
        public EnemyGenerator EnemyGenerator { get; set; }
        public ClockSystem ClockSystem { get; private set; }
        public SaveSystem SaveSystem { get; private set; }

        private bool _loadedExistingSave;

        public override void _Ready()
        {
            Instance = this;

            SaveSystem = new SaveSystem();

            if (SaveSystem.HasSave())
            {
                Save = SaveSystem.LoadGame();

                if (Save == null)
                {
                    GD.PrintErr("Falha ao carregar save.");
                    Save = new SaveData();
                }
                else
                {
                    _loadedExistingSave = true;

                    GD.Print($"Digimons carregados: {Save.Center.Digimons.Count}");

                    // Saves de antes da Enciclopédia (CenterState.DiscoveredDigimonIds)
                    // existir não tinham nada marcado como descoberto - sem isso, toda
                    // espécie que o jogador já tinha antes desse sistema existir apareceria
                    // como "???" pra sempre, mesmo com o Digimon parado bem ali no Center.
                    // MarkDigimonDiscovered é idempotente, seguro de rodar toda vez que o
                    // save carrega.
                    foreach (var d in Save.Center.Digimons)
                    {
                        if (d.BaseData != null)
                            Save.Center.MarkDigimonDiscovered(d.BaseData.Id);
                    }

                    if (!Save.Center.AgeCapMigrationApplied)
                    {
                        ApplyAgeCapMigration();

                        Save.Center.AgeCapMigrationApplied = true;
                    }
                }
            }
            else
            {
                Save = new SaveData();
            }


            CenterService = new CenterService(Save.Center);
            TrainingSystem = new TrainingSystem();
            EggSystem = new EggSystem();
            EnemyGenerator = new EnemyGenerator();

            ClockSystem = new ClockSystem(Save.World);

            ClockSystem.HourPassed += OnHourPassed;
            ClockSystem.DayPassed += OnDayPassed;


            CallDeferred(nameof(InitializeGame));
        }

        private void OnFirstFrame()
        {
            GetTree().ProcessFrame -= OnFirstFrame;

            InitializeNewGame();
        }

        private void InitializeGame()
        {
            if (_loadedExistingSave)
            {
                LoadExistingGame();
            }
            else
            {
                InitializeNewGame();
            }
        }

        public void SaveGame()
        {
            SaveSystem.SaveGame(Save);
        }

        private void LoadExistingGame()
        {
            CenterService = new CenterService(Save.Center);

            MigrateDuplicateGeogreymon();
            ApplyRetroactivePassives();

            PlayerDigimon = CenterService.GetAllDigimons().FirstOrDefault();

            if (PlayerDigimon != null)
            {
                SetPlayerDigimon(PlayerDigimon);
            }

            GD.Print("Save carregado com sucesso!");

            GameLoaded?.Invoke();
        }

        /// <summary>Migração pontual de um erro de dados: existia um "Geogreymon" duplicado
        /// (id 22, ligado ao Agumon clássico) além do "Geogreymon" de verdade (id 44, ex-
        /// "Geogreymon (Savers)", o único que devia existir). O id 22 foi removido do banco
        /// (não existe mais em DatabaseManager), mas saves antigos podem ter uma instância
        /// com BaseData ainda apontando pra ele -
        /// remapeia pra id 44 (mesma Role/Elemento/Atributo/Estágio, então não afeta
        /// CurrentStats/Level/HP/Passiva já sorteada, só a identidade da espécie e o sprite).
        /// Idempotente: só mexe em quem ainda está no id 22.</summary>
        private void MigrateDuplicateGeogreymon()
        {
            const int OldDuplicateGeogreymonId = 22;
            const int RealGeogreymonId = 44;

            var realGeogreymon = DatabaseManager.Instance.GetDigimon(RealGeogreymonId);

            if (realGeogreymon == null)
                return;

            bool anyMigrated = false;

            foreach (var d in Save.Center.Digimons)
            {
                if (d.BaseData != null && d.BaseData.Id == OldDuplicateGeogreymonId)
                {
                    d.BaseData = realGeogreymon;
                    anyMigrated = true;

                    GD.Print($"{d.DisplayName}: migrado do Geogreymon duplicado (id 22) pro Geogreymon atual (id 44).");
                }
            }

            if (anyMigrated)
            {
                SaveGame();
            }
        }

        /// <summary>Saves de antes do sistema de Passivas (ver PASSIVAS_SPEC.md) existir têm
        /// PassiveId nulo em todo Digimon já carregado - sem isso eles ficariam pra sempre
        /// sem passiva, já que só ovos/evoluções novas sorteiam uma. Roda em LoadExistingGame
        /// (via CallDeferred/InitializeGame), depois de DatabaseManager._Ready() já ter
        /// carregado as pools - nunca usa d.BaseData pra isso, que é só uma cópia congelada
        /// de quando o save foi salvo (ver CloneBaseData) e pode não ter a pool de Passives
        /// preenchida. Idempotente: só sorteia pra quem ainda está null.</summary>
        private void ApplyRetroactivePassives()
        {
            bool anyRolled = false;

            foreach (var d in Save.Center.Digimons)
            {
                if (d.PassiveId.HasValue || d.BaseData == null)
                    continue;

                var species = DatabaseManager.Instance.GetDigimon(d.BaseData.Id);

                if (species == null)
                    continue;

                d.PassiveId = PassiveSystem.RollRandomPassive(species);
                anyRolled = true;
            }

            if (anyRolled)
            {
                GD.Print("Passivas retroativas sorteadas para Digimons de save antigo.");

                SaveGame();
            }
        }

        // Só acelera o ClockSystem (relógio/fome/stamina/incubação/doença - tudo que anda por
        // MinutePassed/HourPassed/DayPassed) - sistemas que tickam em tempo real puro fora
        // dele (ex.: regeneração de HP em DigimonWorld) continuam no ritmo normal. Não é
        // salvo no save: some ao reabrir o jogo, é só uma preferência de sessão (ver
        // HUD.OnSpeedButtonPressed).
        public bool IsClockSpeedDoubled { get; private set; }

        public void ToggleClockSpeedDoubled()
        {
            IsClockSpeedDoubled = !IsClockSpeedDoubled;
        }

        public override void _Process(double delta)
        {
            ClockSystem?.Update(delta * (IsClockSpeedDoubled ? 2.0 : 1.0));
        }

        private void OnHourPassed()
        {
            foreach (var digimon in Save.Center.Digimons)
            {
                digimon.AdvanceHour(Save.World);
            }

            EggSystem.AdvanceHour(CenterService);
        }

        private void OnDayPassed()
        {
            // Cópia da lista antes de iterar - EvaluateDailyDeathRisks pode remover o
            // próprio Digimon do Center.Digimons (KillDigimon) no meio do loop, o que
            // quebraria a enumeração original com InvalidOperationException.
            foreach (var digimon in Save.Center.Digimons.ToList())
            {
                digimon.AdvanceDay();
                digimon.TryBecomeSick();

                digimon.DaysSickInARow = digimon.HealthState == HealthState.Sick
                    ? digimon.DaysSickInARow + 1
                    : 0;

                if (EvaluateDailyDeathRisks(digimon))
                    continue;

                TryToEvolve(digimon);
            }

            EggSystem.AdvanceDay(CenterService);
            ApplyRecruitedNpcDailyBonuses();
            SaveGame();
        }

        // Id da Palmon (ver Data/NPCs/npc_palmon.json) - hardcoded porque hoje é o único NPC
        // recrutável com bônus diário; se mais NPCs ganharem bônus desse tipo, isso merece
        // virar dado em NpcData ao invés de const aqui.
        private const int PalmonNpcId = 5;
        private const int PalmonDailyMeatMin = 1;
        private const int PalmonDailyMeatMax = 2;

        /// <summary>Efeitos passivos de NPCs recrutados que tickam uma vez por dia (ver
        /// CenterState.RecruitedNpcIds) - hoje só a Palmon, que deixa Carne de graça no
        /// Center todo dia (não precisa ser alimentada/comprada). O bônus de treino de
        /// Gaogamon/Togemon não mora aqui - é aplicado por tick de treino, ver
        /// HasRecruitedTrainingBonus/DigimonWorld.ProcessTraining.</summary>
        private void ApplyRecruitedNpcDailyBonuses()
        {
            if (!Save.Center.RecruitedNpcIds.Contains(PalmonNpcId))
                return;

            int freeMeat = _random.Next(PalmonDailyMeatMin, PalmonDailyMeatMax + 1);

            Save.Center.Meat += freeMeat;

            GD.Print($"Palmon deixou {freeMeat} Carne(s) de graça no Center.");
        }

        /// <summary>True se algum NPC recrutado (ver CenterState.RecruitedNpcIds) tem
        /// RecruitmentTrainingAreaType igual a essa área - usado por DigimonWorld.
        /// ProcessTraining pra empilhar RecruitedNpcBonusMultiplier em cima do bônus normal
        /// de área específica, e por DigimonWorld.TryStartAutoTraining pra saber que área vale
        /// a pena ir treinar sozinho.</summary>
        public bool HasRecruitedTrainingBonus(CenterAreaType areaType)
        {
            foreach (var npc in DatabaseManager.Instance.GetAllNpcs())
            {
                if (npc.RecruitmentTrainingAreaType == areaType && Save.Center.RecruitedNpcIds.Contains(npc.Id))
                    return true;
            }

            return false;
        }

        // Id do Wizardmon (ver Data/NPCs/npc_wizardmon.json) - hardcoded pelo mesmo motivo
        // de PalmonNpcId acima: hoje é o único NPC que desbloqueia esse perk específico.
        private const int WizardmonNpcId = 7;

        /// <summary>True depois que o Wizardmon é recrutado - só a partir daí o jogador pode
        /// bloquear evoluções indesejadas por Digimon (ver SetEvolutionBlocked). Antes disso
        /// a EvolutionGuideScreen mostra os toggles desabilitados/ocultos.</summary>
        public bool HasRecruitedEvolutionAdvisor =>
            Save.Center.RecruitedNpcIds.Contains(WizardmonNpcId);

        /// <summary>Bloqueia (ou desbloqueia) uma evolução específica pra um Digimon do
        /// roster - ver DigimonInstance.BlockedEvolutionTargetIds/EvolutionSystem.TryToEvolve,
        /// que passa a ignorar esse alvo mesmo que os requisitos sejam atendidos. Exige o
        /// Wizardmon recrutado (HasRecruitedEvolutionAdvisor).</summary>
        public SystemResult SetEvolutionBlocked(DigimonInstance digimon, int targetDigimonId, bool blocked)
        {
            if (digimon == null)
                return SystemResult.Fail("Nenhum Digimon selecionado.");

            if (!HasRecruitedEvolutionAdvisor)
                return SystemResult.Fail("Recrute o Wizardmon antes de poder bloquear evoluções.");

            digimon.SetEvolutionBlocked(targetDigimonId, blocked);

            return SystemResult.Ok();
        }

        /// <summary>True se toda a lista de espécies de um conjunto (ver DigimonSetData) já
        /// foi descoberta pelo menos uma vez (CenterState.DiscoveredDigimonIds) - não exige
        /// nenhuma delas estar viva agora, só ter existido no Center em algum momento. Usado
        /// pela EncyclopediaScreen (aba Conjuntos) e por GetTotalSetDamageBonus.</summary>
        public bool IsSetComplete(DigimonSetData set) =>
            set.DigimonIds.All(id => Save.Center.DiscoveredDigimonIds.Contains(id));

        /// <summary>Soma o EffectValue de todo conjunto completo de um dado SetEffectType -
        /// dois conjuntos completos do mesmo tipo empilham. Usado por DamageCalculator
        /// (FinalDamagePercent/CritChancePercent), ApplyTeamBattleReward (BitsGainedPercent)
        /// e DigimonInstance (HungerDecayReductionPercent/ExperienceGainedPercent). É sempre
        /// um bônus do Center, não da espécie - vale pra qualquer Digimon, não só aos
        /// membros do conjunto.</summary>
        public float GetTotalSetBonus(SetEffectType type) =>
            DatabaseManager.Instance.GetAllSets()
                .Where(s => s.EffectType == type && IsSetComplete(s))
                .Sum(s => s.EffectValue);

        /// <summary>Se o botão de pular sono deveria estar habilitado agora - usado pela HUD.
        /// A janela de sono em si vive em DigimonInstance (hoje sincronizada pro Center
        /// inteiro, sem Digimons diurnos/noturnos ainda) pra não duplicar o hardcode aqui.
        /// Quando esse sistema existir de verdade, SkipSleep precisa ser revisto - "pular"
        /// deixa de fazer sentido do jeito que está se cada Digimon tiver seu próprio ciclo.</summary>
        public bool CanSkipSleep => DigimonInstance.IsSleepHour(Save.World.CurrentHour);

        /// <summary>Disparado quando SkipSleep pula um trecho do relógio, com quantos
        /// segundos de tempo real esse trecho representaria (minutos pulados ×
        /// ClockSystem.SecondsPerGameMinute) - pra sistemas que tickam em tempo real, não em
        /// minutos de jogo (ex.: DigimonWorld.CatchUpPassiveTime, regeneração de HP), poderem
        /// aplicar de uma vez o que teriam acumulado esperando de verdade.</summary>
        public event Action<double> SleepSkipped;

        /// <summary>
        /// Pula da hora de sono atual direto pro momento em que os Digimons acordam - avança
        /// o relógio de verdade minuto a minuto (ClockSystem.AdvanceUntilHour), então nada
        /// que dependa do tempo passando é ignorado (fome, stamina, incubação de ovo, chance
        /// de doença, evolução, save de fim de dia): só a espera em tempo real real, sem
        /// nada pra fazer enquanto todo mundo dorme, deixa de existir. Não faz nada (retorna
        /// false) se não for a hora de sono agora.
        /// </summary>
        public bool SkipSleep()
        {
            if (!DigimonInstance.IsSleepHour(Save.World.CurrentHour))
                return false;

            int minutesSkipped = ClockSystem.AdvanceUntilHour(DigimonInstance.SleepEndHour);

            SleepSkipped?.Invoke(minutesSkipped * ClockSystem.SecondsPerGameMinute);

            return true;
        }

        /// <summary>
        /// Inicialização e log relacionado aos digimons iniciais no centro.
        /// </summary>
        private void InitializeStarterDigimons()
        {
            var db = DatabaseManager.Instance;

            if (db == null)
            {
                GD.PrintErr("DatabaseManager ainda não inicializado!");
                return;
            }

            GD.Print($"Centro criado com {Save.Center.Digimons.Count} Digimons.");
        }

        /// <summary>
        /// Seleciona o digimon do jogador a partir do Center pelo ID e reconstrói o sistema de batalha.
        /// </summary>
        public void SetPlayerDigimon(DigimonInstance digimon)
        {
            if (digimon == null)
            {
                GD.PrintErr("Digimon inválido.");
                return;
            }

            PlayerDigimon = digimon;

            PlayerDigimonChanged?.Invoke();
        }

        /// <summary>
        /// Avança o tempo global do jogo (dias) e aplica avanço nos digimons do Center.
        /// Também tenta evoluir o digimon do jogador quando apropriado.
        /// </summary>
        public void SkipDay()
        {
            Save.World.CurrentHour = 1;
            Save.World.CurrentMinute = 59;
        }

        public SystemResult FeedPlayer()
        {
            return FeedDigimon(PlayerDigimon);
        }

        public SystemResult FeedDigimon(DigimonInstance digimon)
        {
            if (Save.Center.Meat <= 0)
            {
                return SystemResult.Fail("Você não possui Comida.");
            }

            if (digimon.Hunger >= digimon.MaxHunger)
                return SystemResult.Fail("O Digimon não está com fome.");

            Save.Center.Meat--;

            digimon.Feed(10);

            return SystemResult.Ok("O Digimon foi alimentado.");
        }

        public void SetPlayerDigimonInstance(DigimonInstance digimon)
        {
            if (digimon == null)
            {
                GD.PrintErr("Tentativa de selecionar Digimon nulo.");
                return;
            }

            PlayerDigimon = digimon;

            PlayerDigimonChanged?.Invoke();
        }

        /// <summary>
        /// Executa treino no <see cref="PlayerDigimon"/> usando o <see cref="TrainingSystem"/>.
        /// Em caso de sucesso aplica o resultado e tenta evolução.
        /// </summary>
        public SystemResult TrainPlayer(TrainingType type)
        {
            if (PlayerDigimon == null)
            {
                return SystemResult.Fail("Nenhum Digimon selecionado.");
            }

            if (PlayerDigimon.HealthState == HealthState.Sick)
            {
                return SystemResult.Fail("O Digimon está doente.");
            }

            var result = TrainingSystem.Execute(PlayerDigimon, type);

            if (!result.Success)
            {
                return SystemResult.Fail(result.Reason);
            }

            PlayerDigimon.ApplyTrainingResult(result);

            TryToEvolve(PlayerDigimon);

            return SystemResult.Ok();
        }

        /// <summary>
        /// Tenta evoluir o digimon passado utilizando a lógica do <see cref="EvolutionSystem"/>.
        /// Se os requisitos forem atendidos mas faltar capacidade no Center, não evolui e
        /// dispara <see cref="EvolutionBlockedByCapacity"/> (uma vez só, até o jogador decidir
        /// manter sem evoluir ou liberar capacidade) em vez de travar o Digimon por baixo dos
        /// panos - ver DigimonInstance.EvolutionCapacityWarningDismissed.
        /// </summary>
        public bool TryToEvolve(DigimonInstance digimon)
        {
            if (digimon == null)
                return false;

            var attempt = EvolutionSystem.TryToEvolve(
                digimon,
                Save.Center.CapacityUsed,
                Save.Center.CapacityLimit
            );

            switch (attempt.Outcome)
            {
                case EvolutionOutcome.Evolved:
                    digimon.EvolutionCapacityWarningDismissed = false;
                    Save.Center.MarkDigimonDiscovered(attempt.TargetForm.Id);
                    return true;

                case EvolutionOutcome.BlockedByCapacity:
                    if (!digimon.EvolutionCapacityWarningDismissed)
                    {
                        EvolutionBlockedByCapacity?.Invoke(
                            digimon,
                            attempt.TargetForm,
                            attempt.CapacityDeficit
                        );
                    }

                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Tenta fundir dois Digimons do roster num Digimon novo via Jogress (ver
        /// JogressSystem.TryToFuse) - sem requisito de nível/stats, só precisa dos dois
        /// Digimons certos e capacidade suficiente no Center. Se bloqueado por capacidade
        /// ou se a combinação não corresponder a nenhuma receita, nada muta - os dois
        /// continuam no Center exatamente como estavam.
        /// </summary>
        public JogressAttemptResult TryToFuse(DigimonInstance a, DigimonInstance b)
        {
            if (a == null || b == null || a == b)
                return JogressAttemptResult.NotEligible();

            var attempt = JogressSystem.TryToFuse(
                a,
                b,
                Save.Center.CapacityUsed,
                Save.Center.CapacityLimit
            );

            if (attempt.Outcome != JogressOutcome.Fused)
                return attempt;

            CenterService.RemoveDigimon(a);
            CenterService.RemoveDigimon(b);

            // AddDigimon já chama MarkDigimonDiscovered sozinho (ver CenterState.AddDigimon) -
            // não repetir aqui.
            CenterService.AddDigimon(attempt.FusedDigimon);

            if (PlayerDigimon == a || PlayerDigimon == b)
                SetPlayerDigimonInstance(attempt.FusedDigimon);

            // JogressFused primeiro - quem desenha o Center usa ele pra achar a posição
            // dos dois DigimonWorld de origem (ver Center.OnJogressFused) e nascer o
            // fundido ali, ANTES de DigimonDeleted apagar esses dois visuais. Depois,
            // reaproveita o mesmo evento que DeleteDigimon/KillDigimon usam pros dois
            // Digimons de origem - Center.OnDigimonDeleted já está inscrito nele e remove
            // o DigimonWorld visual correspondente, sem precisar de nenhum código novo de
            // despawn (os dois SAEM do roster de vez, é a mesma semântica).
            JogressFused?.Invoke(a, b, attempt.FusedDigimon);

            DigimonDeleted?.Invoke(a);
            DigimonDeleted?.Invoke(b);

            SaveGame();

            return attempt;
        }

        /// <summary>
        /// Inicialização de novo jogo: cria ovo inicial via <see cref="EggSystem"/>. Um save
        /// novo nunca tem Digimon já no Center nesse ponto (só o ovo) - é HatchEgg quem
        /// define o PlayerDigimon, quando o ovo choca de verdade (ver EggSystem.HatchEgg).
        /// </summary>
        public void InitializeNewGame()
        {
            EggSystem.CreateInitialEgg(CenterService);

            SaveSystem.SaveGame(Save);
        }

        /// <summary>Debug: reproduz o cenário relatado de Suporte vs Suporte (mesmo
        /// SupportType dos dois lados) num 1x1, pra confirmar ao vivo que o fallback de
        /// ataque (ver BattleUnit.SelectTarget/_supportFallbackAttack) resolve o impasse em
        /// vez de travar pra sempre. Só usado via DEBUG_TEST_BATTLE=support_deadlock_{healer,
        /// buffer,debuffer} (ver Center.cs).</summary>
        public void DebugStartSupportDeadlockBattle(SupportType supportType)
        {
            var supports = DatabaseManager.Instance.GetAllDigimons()
                .Where(d => d.Role == RoleType.Support && d.SupportType == supportType)
                .Take(2)
                .ToList();

            if (supports.Count < 2)
            {
                GD.PrintErr($"DebugStartSupportDeadlockBattle: não há 2 Digimons {supportType} no banco.");
                return;
            }

            GD.Print($"DEBUG: batalha de teste Suporte vs Suporte ({supportType}) - {supports[0].Name} vs {supports[1].Name}");

            var playerTeam = new List<DigimonInstance> { new DigimonInstance(supports[0]) };
            var enemyTeam = new List<DigimonInstance> { new DigimonInstance(supports[1]) };

            ActiveTournament = null;
            BattleFromExploration = false;

            LaunchBattle(playerTeam, enemyTeam);
        }

        /// <summary>
        /// Inicia uma batalha em tempo real com o time escolhido pelo jogador (1 Digimon pra
        /// um duelo 1x1, ou 3 pro 3x3 completo - sem restrição de Role). A mesma arena/lógica
        /// de combate atende os dois: o tamanho do time inimigo gerado acompanha o do jogador.
        /// Gera o time inimigo calibrado por esse time e abre a arena; a recompensa é aplicada
        /// quando a arena avisa que a batalha terminou.
        /// </summary>
        public void StartTeamBattle(List<DigimonInstance> playerTeam)
        {
            if (IsBattleActive)
            {
                GD.PrintErr("StartTeamBattle: já existe uma batalha em andamento, ignorando.");
                return;
            }

            if (playerTeam == null || (playerTeam.Count != 1 && playerTeam.Count != 3))
            {
                GD.PrintErr("StartTeamBattle exige 1 Digimon (1x1) ou 3 Digimons (3x3).");
                return;
            }

            var enemyTeam = EnemyGenerator.GenerateEnemyTeam(playerTeam, playerTeam.Count);

            if (enemyTeam.Count == 0)
            {
                GD.PrintErr("Não foi possível gerar o time inimigo.");
                return;
            }

            ActiveTournament = null;
            BattleFromExploration = false;

            LaunchBattle(playerTeam, enemyTeam);
        }

        /// <summary>
        /// Inicia a batalha de um campeonato (ver TournamentData): diferente da batalha
        /// livre, o time inimigo é fixo - não calibrado pelo poder do time do jogador -, e o
        /// tamanho do time do jogador precisa bater exatamente com o número de oponentes
        /// cadastrados (1x1 ou 3x3, dependendo do campeonato).
        /// </summary>
        public void StartTournamentBattle(TournamentData tournament, List<DigimonInstance> playerTeam)
        {
            if (IsBattleActive)
            {
                GD.PrintErr("StartTournamentBattle: já existe uma batalha em andamento, ignorando.");
                return;
            }

            if (tournament == null)
                return;

            if (playerTeam == null || playerTeam.Count != tournament.Opponents.Count)
            {
                GD.PrintErr($"StartTournamentBattle exige {tournament.Opponents.Count} Digimon(s) pra '{tournament.Name}'.");
                return;
            }

            var enemyTeam = BuildTournamentEnemyTeam(tournament);

            if (enemyTeam.Count != tournament.Opponents.Count)
            {
                GD.PrintErr($"Não foi possível montar o time do campeonato '{tournament.Name}'.");
                return;
            }

            ActiveTournament = tournament;
            BattleFromExploration = false;

            LaunchBattle(playerTeam, enemyTeam);
        }

        /// <summary>
        /// Inicia um combate 1x1 contra um selvagem encontrado numa área de exploração (ver
        /// ExplorationArea/WildEncounterSpawn) - diferente de campeonato, não aplica nenhum
        /// bônus de treino no oponente (selvagens devem continuar fáceis de grindar, é a
        /// principal fonte de XP fora de treino/batalha livre). Vitória avança o progresso de
        /// qualquer quest DefeatWild compatível (ver ApplyWildEncounterQuestProgress).
        /// </summary>
        public void StartWildEncounter(DigimonInstance explorer, int wildDigimonId, int level)
        {
            if (IsBattleActive)
            {
                GD.PrintErr("StartWildEncounter: já existe uma batalha em andamento, ignorando.");
                return;
            }

            if (explorer == null)
                return;

            var data = DatabaseManager.Instance.GetDigimon(wildDigimonId);

            if (data == null)
            {
                GD.PrintErr($"StartWildEncounter: Digimon {wildDigimonId} não encontrado.");
                return;
            }

            var wild = new DigimonInstance(data);

            // Selvagem de exploração também sorteia passiva (ver PASSIVAS_SPEC.md R5) - mesma
            // regra de Batalha Livre, só que fora do EnemyGenerator (esse encontro é 1x1 e
            // monta a instância direto aqui).
            wild.PassiveId = PassiveSystem.RollRandomPassive(data);

            while (wild.Level < level)
            {
                wild.GainExperience(wild.ExperienceToNextLevel);
            }

            wild.RestoreHealth();

            ActiveTournament = null;
            BattleFromExploration = true;
            _activeWildEncounterDigimonId = wildDigimonId;
            _resolvedWildEncounterDrops = null;

            LaunchBattle(new List<DigimonInstance> { explorer }, new List<DigimonInstance> { wild });
        }

        private List<DigimonInstance> BuildTournamentEnemyTeam(TournamentData tournament)
        {
            var enemies = new List<DigimonInstance>();

            foreach (var opponent in tournament.Opponents)
            {
                var data = DatabaseManager.Instance.GetDigimon(opponent.DigimonId);

                if (data == null)
                {
                    GD.PrintErr($"Digimon {opponent.DigimonId} do campeonato '{tournament.Name}' não encontrado.");
                    continue;
                }

                var enemy = new DigimonInstance(data);

                // Torneio usa passiva FIXA definida por design (ver PASSIVAS_SPEC.md R5),
                // nunca sorteada - se o campeonato não especificou, o oponente fica sem
                // passiva (não sorteia como fallback).
                enemy.PassiveId = opponent.PassiveId;

                while (enemy.Level < opponent.Level)
                {
                    enemy.GainExperience(enemy.ExperienceToNextLevel);
                }

                enemy.RestoreHealth();
                EnemyGenerator.ApplyTournamentTrainingBonus(enemy);

                enemies.Add(enemy);
            }

            return enemies;
        }

        /// <summary>
        /// Setup compartilhado por StartTeamBattle e StartTournamentBattle: sobe a arena por
        /// cima da cena atual e pausa o resto do jogo (ver comentário original abaixo).
        /// </summary>
        private void LaunchBattle(List<DigimonInstance> playerTeam, List<DigimonInstance> enemyTeam)
        {
            PlayerBattleTeam = playerTeam;
            EnemyBattleTeam = enemyTeam;

            // Registra quem entrou fraco demais na luta (ver DigimonInstance.
            // TimesEnteredBattleAtLowHealth) - a morte por isso só é avaliada depois, junto
            // com o resto dos riscos de maus-tratos pós-batalha (ApplyTeamBattleReward), pra
            // não precisar mexer no time já escalado no meio da montagem da arena.
            foreach (var member in PlayerBattleTeam)
            {
                if (member.CurrentHealthPoints < member.MaxHealthPoints * LowHealthBattleThreshold)
                    member.TimesEnteredBattleAtLowHealth++;
            }

            TeamBattleStarted?.Invoke();

            var arenaScene = GD.Load<PackedScene>("res://Scenes/Battle/BattleArena.tscn");
            var arena = arenaScene.Instantiate<BattleArena>();

            // A arena é adicionada por cima da cena atual (Center continua existindo por
            // baixo, não é trocada) - sem pausar a árvore, o Center seguia processando fome/
            // regeneração de HP dos Digimons do time do jogador (os MESMOS DigimonInstance
            // usados na batalha) por trás da luta. Isso fazia um Digimon já derrotado (HP 0)
            // voltar a ter HP>0 por causa da regeneração passiva do Center, virando alvo
            // válido nas checagens de "está vivo" da batalha de novo - o time inimigo então
            // trocava de alvo pra ele, "matava" de novo, e o ciclo podia se repetir. Pausar a
            // árvore inteira (com a arena marcada como Always, pra continuar processando por
            // cima da pausa) resolve isso na raiz.
            arena.ProcessMode = ProcessModeEnum.Always;

            GetTree().Root.AddChild(arena);

            RequestPause("battle");

            arena.Init(PlayerBattleTeam, EnemyBattleTeam);

            BattleMatch = arena.Match;

            arena.BattleFinished += result => OnTeamBattleFinished(result, arena);
        }

        private void OnTeamBattleFinished(BattleResult result, BattleArena arena)
        {
            ReleasePause("battle");

            ApplyTeamBattleReward(result);

            arena.QueueFree();

            BattleMatch = null;

            // BattleFromExploration precisa continuar valendo até aqui - é o que diz pra
            // Center.cs/ExplorationArea qual dos dois deve voltar a se mostrar (ver
            // Center.OnTeamBattleStarted/Finished). Só reseta depois que os dois já reagiram.
            TeamBattleFinished?.Invoke(result);

            BattleFromExploration = false;
            _activeWildEncounterDigimonId = null;
            _resolvedWildEncounterDrops = null;
        }

        /// <summary>
        /// Aplica o resultado de uma batalha 3x3: na derrota, todo o time perde Felicidade;
        /// na vitória, cada membro (sobrevivente ou não) recebe sua fração de XP e o bônus
        /// de Felicidade, e tenta evoluir; os Bits vão pro Center uma única vez.
        /// </summary>
        // Fração do XP normal que o time ganha mesmo perdendo - sem isso, um time que está
        // ficando pra trás nunca acumula XP suficiente pra evoluir e virar o jogo, ficando
        // travado sem conseguir progredir. Público pra BattleArena usar o mesmo valor ao
        // pré-visualizar a recompensa na tela de resultado (ver BattleArena._Process).
        public const int LossExperienceDivisor = 5;

        /// <summary>Campeonato da batalha em andamento, ou nulo se for batalha livre - setado
        /// por StartTournamentBattle, consumido (e zerado) em ApplyTeamBattleReward. Público
        /// pra BattleArena consultar ao montar a prévia de recompensa.</summary>
        public TournamentData ActiveTournament { get; private set; }

        public void ApplyTeamBattleReward(BattleResult result)
        {
            if (PlayerBattleTeam.Count == 0)
            {
                ActiveTournament = null;
                return;
            }

            // Campeonato não dá XP (nem na vitória, nem no XP de consolação da derrota) -
            // só Bits/capacidade/bônus. Sem essa restrição, dava pra farmar XP infinito
            // perdendo de propósito pro time inimigo de um campeonato forte (a tentativa não
            // é "gasta" numa derrota, então era repetível à vontade).
            bool isTournament = ActiveTournament != null;

            if (result == BattleResult.EnemyWon)
            {
                int consolationXp = 0;

                if (!isTournament)
                {
                    var lossReward = BattleRewardCalculator.CalculateForTeam(PlayerBattleTeam, EnemyBattleTeam);
                    consolationXp = lossReward.Experience / LossExperienceDivisor;
                }

                GD.Print($"Derrota! +{consolationXp} XP de consolação por membro, sem Bits.");

                foreach (var member in PlayerBattleTeam.ToList())
                {
                    member.RecordBattleResult(won: false);
                    member.ConsecutiveBattleLosses++;

                    if (member.CurrentHealthPoints <= 0)
                        member.TimesKnockedOutInBattle++;

                    member.ChangeHappiness(-6);

                    if (EvaluatePostBattleMistreatmentRisk(member))
                        continue;

                    if (consolationXp > 0)
                    {
                        member.GainExperience(consolationXp);

                        TryToEvolve(member);
                    }
                }

                // Perder um campeonato não dá a recompensa de capacidade, mas também não
                // "gasta" a tentativa - o campeonato continua disponível pra tentar de novo.
                ActiveTournament = null;
                return;
            }

            var reward = BattleRewardCalculator.CalculateForTeam(PlayerBattleTeam, EnemyBattleTeam);

            int xpGained = isTournament ? 0 : reward.Experience;

            GD.Print($"Vitória! +{xpGained} XP por membro | +{reward.Bits} Bits");

            foreach (var member in PlayerBattleTeam.ToList())
            {
                member.RecordBattleResult(won: true);
                member.ConsecutiveBattleLosses = 0;

                if (member.CurrentHealthPoints <= 0)
                    member.TimesKnockedOutInBattle++;

                if (xpGained > 0)
                    member.GainExperience(xpGained);

                member.ChangeHappiness(8);

                if (EvaluatePostBattleMistreatmentRisk(member))
                    continue;

                TryToEvolve(member);
            }

            int bitsWithSetBonus = (int)(reward.Bits * (1f + GetTotalSetBonus(SetEffectType.BitsGainedPercent)));

            Save.Center.AddBits(bitsWithSetBonus);

            ApplyTournamentRewardIfNeeded();

            if (BattleFromExploration && _activeWildEncounterDigimonId.HasValue)
            {
                ApplyWildEncounterQuestProgress(_activeWildEncounterDigimonId.Value);
                ApplyResolvedWildEncounterDrops();
            }

            ActiveTournament = null;
        }

        /// <summary>
        /// Depois de vencer um encontro selvagem (ver StartWildEncounter), avança o progresso
        /// de qualquer quest DefeatWild aceita cujo alvo bata com o Digimon derrotado
        /// (ObjectiveTargetId 0 = qualquer selvagem conta). Completar a contagem não entrega a
        /// quest sozinho, só deixa pronta - a entrega acontece ao voltar no NPC (ver
        /// TryTurnInQuest).
        /// </summary>
        private void ApplyWildEncounterQuestProgress(int defeatedDigimonId)
        {
            foreach (var questId in Save.Center.ActiveQuestIds)
            {
                var quest = DatabaseManager.Instance.GetQuest(questId);

                if (quest == null || quest.ObjectiveType != QuestObjectiveType.DefeatWild)
                    continue;

                if (quest.ObjectiveTargetId != 0 && quest.ObjectiveTargetId != defeatedDigimonId)
                    continue;

                int current = Save.Center.QuestProgress.GetValueOrDefault(questId);

                Save.Center.QuestProgress[questId] = Math.Min(current + 1, quest.ObjectiveCount);
            }
        }

        // Resultado do sorteio de drops do encontro selvagem em andamento - resolvido uma
        // única vez (ver ResolveWildEncounterDrops) porque a prévia de recompensa (BattleArena,
        // mostrada antes do jogador clicar OK) e a aplicação de verdade (aqui embaixo)
        // precisam concordar no que caiu. Sortear duas vezes independentes mostraria um item
        // na prévia e daria outro (ou nenhum) de verdade.
        private List<(ItemData Item, int Quantity)> _resolvedWildEncounterDrops;

        /// <summary>
        /// Sorteia (na primeira chamada) os drops da espécie selvagem derrotada no encontro em
        /// andamento (ver DigimonData.Drops - lookup pela espécie "de ficha" no banco, não pelo
        /// BaseData clonado da instância, que não carrega Drops) e devolve o resultado. Chamadas
        /// seguintes reaproveitam o mesmo sorteio - não aplica nada sozinho, só resolve o
        /// "o que caiu" pra quem chamar (prévia ou aplicação de verdade) decidir o que fazer.
        /// </summary>
        public List<(ItemData Item, int Quantity)> ResolveWildEncounterDrops()
        {
            if (_resolvedWildEncounterDrops != null)
                return _resolvedWildEncounterDrops;

            _resolvedWildEncounterDrops = new List<(ItemData, int)>();

            if (!BattleFromExploration || !_activeWildEncounterDigimonId.HasValue)
                return _resolvedWildEncounterDrops;

            var data = DatabaseManager.Instance.GetDigimon(_activeWildEncounterDigimonId.Value);

            if (data == null)
                return _resolvedWildEncounterDrops;

            foreach (var drop in data.Drops)
            {
                if (_random.NextDouble() * 100.0 >= drop.ChancePercent)
                    continue;

                var itemData = DatabaseManager.Instance.GetItem(drop.ItemId);

                if (itemData == null)
                    continue;

                _resolvedWildEncounterDrops.Add((itemData, drop.Quantity));
            }

            return _resolvedWildEncounterDrops;
        }

        /// <summary>Aplica de verdade o sorteio de ResolveWildEncounterDrops - soma cada item no
        /// inventário do Center e avança quest CollectItem compatível.</summary>
        private void ApplyResolvedWildEncounterDrops()
        {
            foreach (var (item, quantity) in ResolveWildEncounterDrops())
            {
                Save.Center.AddItemQuantity(item.Id, quantity);

                ApplyCollectItemQuestProgress(item.Id);

                GD.Print($"Drop: {item.Name} x{quantity}.");
            }
        }

        /// <summary>Avança quest CollectItem ativa compatível com um item que acabou de entrar
        /// no inventário - usado tanto por item coletado no chão da exploração (ver
        /// CollectExplorationItem) quanto por drop de selvagem (ver ApplyWildEncounterDrops).</summary>
        private void ApplyCollectItemQuestProgress(int itemId)
        {
            foreach (var questId in Save.Center.ActiveQuestIds)
            {
                var quest = DatabaseManager.Instance.GetQuest(questId);

                if (quest == null || quest.ObjectiveType != QuestObjectiveType.CollectItem)
                    continue;

                if (quest.ObjectiveTargetId != itemId)
                    continue;

                int current = Save.Center.QuestProgress.GetValueOrDefault(questId);

                Save.Center.QuestProgress[questId] = Math.Min(current + 1, quest.ObjectiveCount);
            }
        }

        /// <summary>
        /// Recompensa de progressão dos campeonatos, concedida só na primeira vitória de cada
        /// um (ver CenterState.ClearedTournamentIds) - vitórias seguintes ainda dão o XP/Bits
        /// normais da batalha, só não repetem esse bônus. Cada campeonato define capacidade
        /// e/ou Bits de bônus (TournamentData.CapacityReward/BitsReward); nem todos precisam
        /// dar os dois.
        /// </summary>
        private void ApplyTournamentRewardIfNeeded()
        {
            if (ActiveTournament == null)
                return;

            if (Save.Center.ClearedTournamentIds.Contains(ActiveTournament.Id))
                return;

            Save.Center.ClearedTournamentIds.Add(ActiveTournament.Id);

            if (ActiveTournament.CapacityReward > 0)
                Save.Center.AddCapacity(ActiveTournament.CapacityReward);

            if (ActiveTournament.BitsReward > 0)
                Save.Center.AddBits(ActiveTournament.BitsReward);

            GD.Print(
                $"Campeonato '{ActiveTournament.Name}' concluído! " +
                $"+{ActiveTournament.CapacityReward} de capacidade | +{ActiveTournament.BitsReward} Bits de bônus."
            );
        }

        /// <summary>Área de exploração atualmente aberta, ou nula fora de exploração - setada
        /// por StartExploration, zerada por EndExploration.</summary>
        public ExplorationArea ActiveExplorationArea { get; private set; }

        /// <summary>True enquanto uma área de exploração está aberta - StartExploration recusa
        /// abrir uma nova enquanto isso for true, mesma proteção (e mesmo motivo, ver
        /// IsBattleActive) usada em StartTeamBattle/StartTournamentBattle/StartWildEncounter:
        /// um clique perdido vazando pro ExplorationDigimonPickScreen escondido atrás da área
        /// recém-aberta chamaria StartExploration de novo, empilhando uma segunda ExplorationArea
        /// por cima e perdendo a referência da primeira em ActiveExplorationArea.</summary>
        public bool IsExploring => ActiveExplorationArea != null;

        public event Action ExplorationStarted;
        public event Action ExplorationFinished;

        /// <summary>
        /// Abre uma área de exploração com o Digimon escolhido (só 1 por vez - ver
        /// game_idea.txt) - mesmo padrão de LaunchBattle: sobe a cena por cima do Center e
        /// pausa o resto do jogo. ExplorationArea.Init sorteia os selvagens da área a partir
        /// de ExplorationMapData.WildPool.
        /// </summary>
        public void StartExploration(DigimonInstance explorer, ExplorationMapData map)
        {
            if (IsExploring)
            {
                GD.PrintErr("StartExploration: já existe uma exploração em andamento, ignorando.");
                return;
            }

            if (explorer == null || map == null)
                return;

            ExplorationStarted?.Invoke();

            var areaScene = GD.Load<PackedScene>("res://Scenes/Exploration/ExplorationArea.tscn");
            var area = areaScene.Instantiate<ExplorationArea>();

            area.ProcessMode = ProcessModeEnum.Always;

            GetTree().Root.AddChild(area);

            RequestPause("exploration");

            area.Init(explorer, map);

            ActiveExplorationArea = area;
        }

        /// <summary>Sai da área de exploração de volta pro Center - chamado pelo botão de
        /// saída da própria ExplorationArea.</summary>
        public void EndExploration()
        {
            if (ActiveExplorationArea != null)
            {
                ActiveExplorationArea.QueueFree();
                ActiveExplorationArea = null;
            }

            ReleasePause("exploration");

            ExplorationFinished?.Invoke();
        }

        /// <summary>
        /// Coleta um item do chão numa área de exploração - soma no inventário do Center e
        /// marca o pickup como coletado (não volta a aparecer nessa área, ver
        /// CenterState.CollectedItemPickupIds). Avança quest CollectItem compatível.
        /// </summary>
        public SystemResult CollectExplorationItem(ItemPickupData pickup)
        {
            if (pickup == null)
                return SystemResult.Fail("Item inválido.");

            if (Save.Center.CollectedItemPickupIds.Contains(pickup.UniqueId))
                return SystemResult.Fail("Esse item já foi coletado.");

            Save.Center.AddItemQuantity(pickup.ItemId, pickup.Quantity);
            Save.Center.CollectedItemPickupIds.Add(pickup.UniqueId);

            ApplyCollectItemQuestProgress(pickup.ItemId);

            return SystemResult.Ok();
        }

        public SystemResult TryAcceptQuest(int questId)
        {
            var quest = DatabaseManager.Instance.GetQuest(questId);

            if (quest == null)
                return SystemResult.Fail("Quest não encontrada.");

            if (Save.Center.CompletedQuestIds.Contains(questId))
                return SystemResult.Fail("Você já completou essa quest.");

            if (Save.Center.ActiveQuestIds.Contains(questId))
                return SystemResult.Fail("Você já aceitou essa quest.");

            Save.Center.ActiveQuestIds.Add(questId);
            Save.Center.QuestProgress[questId] = 0;

            return SystemResult.Ok();
        }

        /// <summary>Progresso atual de uma quest aceita em direção ao objetivo (0 se não foi
        /// aceita ainda).</summary>
        public int GetQuestProgress(int questId)
        {
            return Save.Center.QuestProgress.GetValueOrDefault(questId);
        }

        public bool IsQuestReadyToTurnIn(int questId)
        {
            var quest = DatabaseManager.Instance.GetQuest(questId);

            if (quest == null || !Save.Center.ActiveQuestIds.Contains(questId))
                return false;

            return GetQuestProgress(questId) >= quest.ObjectiveCount;
        }

        /// <summary>Entrega uma quest pronta (ver IsQuestReadyToTurnIn) - aplica a recompensa
        /// (Bits/Capacidade/item) e marca como completa.</summary>
        public SystemResult TryTurnInQuest(int questId)
        {
            var quest = DatabaseManager.Instance.GetQuest(questId);

            if (quest == null)
                return SystemResult.Fail("Quest não encontrada.");

            if (!IsQuestReadyToTurnIn(questId))
                return SystemResult.Fail("O objetivo dessa quest ainda não foi cumprido.");

            Save.Center.ActiveQuestIds.Remove(questId);
            Save.Center.QuestProgress.Remove(questId);
            Save.Center.CompletedQuestIds.Add(questId);

            if (quest.RewardBits > 0)
                Save.Center.AddBits(quest.RewardBits);

            if (quest.RewardCapacity > 0)
                Save.Center.AddCapacity(quest.RewardCapacity);

            if (quest.RewardItemId > 0 && quest.RewardItemQuantity > 0)
                Save.Center.AddItemQuantity(quest.RewardItemId, quest.RewardItemQuantity);

            // NPC recrutável: entregar a quest dele conta como "provou seu valor" e o recruta
            // pro Center (ver NpcData.IsRecruitable/CenterState.RecruitedNpcIds) - efeitos
            // reais consultam essa lista sob demanda (GameManager.HasRecruitedTrainingBonus/
            // ApplyRecruitedNpcDailyBonuses, DigimonWorld.TryStartAutoTraining), não precisam
            // ser disparados daqui.
            var giverNpc = DatabaseManager.Instance.GetNpc(quest.GiverNpcId);

            if (giverNpc != null && giverNpc.IsRecruitable && !Save.Center.RecruitedNpcIds.Contains(giverNpc.Id))
                Save.Center.RecruitedNpcIds.Add(giverNpc.Id);

            return SystemResult.Ok();
        }

        public SystemResult BuyMeat()
        {
            if (Save.Center.Bits < 20)
                return SystemResult.Fail("Bits insuficientes.");

            Save.Center.Bits -= 20;
            Save.Center.Meat++;

            return SystemResult.Ok();
        }

        public SystemResult BuyMedicine()
        {
            if (Save.Center.Bits < 100)
                return SystemResult.Fail("Bits insuficientes.");

            Save.Center.Bits -= 100;
            Save.Center.Medicine++;

            return SystemResult.Ok();
        }

        public const int StaminaSnackPrice = 2000;

        public SystemResult BuyStaminaSnack()
        {
            if (Save.Center.Bits < StaminaSnackPrice)
                return SystemResult.Fail("Bits insuficientes.");

            Save.Center.Bits -= StaminaSnackPrice;
            Save.Center.StaminaSnacks++;

            return SystemResult.Ok();
        }

        /// <summary>Custo de capacidade que o próximo ovo comprado vai reservar - todo
        /// Digimon Baby custa o mesmo (ver DigimonInstance.GetCapacityCostForStage), então
        /// basta olhar o primeiro da lista. Usado tanto por BuyEgg quanto pela loja pra
        /// decidir se mostra o botão de comprar habilitado antes mesmo de tentar.</summary>
        public bool HasCapacityForEgg()
        {
            var babyDigimons = DatabaseManager.Instance.GetAllDigimons()
                .Where(d => d.Stage == DigimonStage.Baby)
                .ToList();

            if (babyDigimons.Count == 0)
                return false;

            int cost = DigimonInstance.GetCapacityCostForStage(babyDigimons[0].Stage);

            return Save.Center.CapacityUsed + cost <= Save.Center.CapacityLimit;
        }

        /// <summary>Preço de um ovo "aleatório" (qualquer Digimon Baby, de qualquer EggType).
        /// Comprar um tipo específico (ver GetEggPrice/BuyEgg) custa
        /// SpecificEggPriceMultiplier vezes mais - o jogador paga pela certeza.</summary>
        public const int RandomEggPrice = 500;
        public const int SpecificEggPriceMultiplier = 3;

        public static int GetEggPrice(EggType? eggType) =>
            eggType.HasValue ? RandomEggPrice * SpecificEggPriceMultiplier : RandomEggPrice;

        /// <summary>Quantos Digimon Baby existem no DB pra um EggType específico - usado pela
        /// tela de escolha de tipo (ShopScreen) pra desabilitar/avisar tipos sem nenhum Baby
        /// cadastrado, em vez de deixar o jogador pagar 3x por um ovo que nunca vai chocar
        /// (hoje todo EggType tem pelo menos um Baby, mas o Digimon fica pra trás quando um
        /// EggType novo é adicionado sem Baby correspondente).</summary>
        public int CountBabyDigimonsOfType(EggType eggType) =>
            DatabaseManager.Instance.GetAllDigimons()
                .Count(d => d.Stage == DigimonStage.Baby && d.EggType == eggType);

        /// <summary>
        /// Compra um ovo - <paramref name="eggType"/> nulo sorteia entre todo Digimon Baby
        /// (preço normal); um EggType específico restringe o sorteio só aos Babies daquele
        /// tipo (preço 3x, ver GetEggPrice). Em ambos os casos ainda é um sorteio dentro do
        /// tipo escolhido, não um Digimon exato - só o EggType é garantido.
        /// </summary>
        public SystemResult BuyEgg(EggType? eggType = null)
        {
            int eggPrice = GetEggPrice(eggType);

            if (Save.Center.Bits < eggPrice)
            {
                return SystemResult.Fail("Bits insuficientes.");
            }

            var babyDigimons = DatabaseManager.Instance.GetAllDigimons()
                .Where(d => d.Stage == DigimonStage.Baby && (!eggType.HasValue || d.EggType == eggType.Value))
                .ToList();

            if (babyDigimons.Count == 0)
            {
                return SystemResult.Fail(
                    eggType.HasValue
                        ? $"Nenhum Digimon Baby do tipo {eggType.Value} disponível."
                        : "Nenhum Digimon Baby disponível."
                );
            }

            var chosen = babyDigimons[GD.RandRange(0, babyDigimons.Count - 1)];

            // Confere a capacidade com uma instância descartável, só para checar o custo pelo estágio.
            var previewDigimon = new DigimonInstance(chosen);

            if (!CenterService.CanAddDigimon(previewDigimon))
            {
                return SystemResult.Fail("Não há capacidade suficiente no Center para outro ovo.");
            }

            bool created = EggSystem.CreateEgg(
                chosen.Id,
                CenterService
            );

            if (!created)
            {
                return SystemResult.Fail("Não foi possível criar o ovo.");
            }

            Save.Center.Bits -= eggPrice;

            return SystemResult.Ok();
        }

        /// <summary>Preço das áreas "base" (Treino genérico/Dormitório/Restaurante/Hospital).</summary>
        public const int AreaPrice = 1500;

        /// <summary>Preço das áreas de treino especializadas por stat (ver CenterArea.
        /// GetForcedTrainingType) - mais caras que a área de treino genérica, já que dão um
        /// bônus permanente de treino (ver TrainingSystem.SpecificAreaBonusMultiplier).</summary>
        public const int SpecificTrainingAreaPrice = 5000;

        /// <summary>Preço de compra de um tipo de área, na loja.</summary>
        public static int GetAreaPrice(CenterAreaType areaType) =>
            CenterArea.GetForcedTrainingType(areaType).HasValue
                ? SpecificTrainingAreaPrice
                : AreaPrice;

        /// <summary>
        /// Compra uma área nova - só cobra os Bits (preço depende do tipo, ver GetAreaPrice).
        /// O posicionamento em si (escolher em qual hexágono disponível ela entra) acontece
        /// depois, no Center (ver Center.StartAreaPlacement, disparado pelo evento
        /// ShopScreen.AreaPurchased).
        /// </summary>
        public SystemResult BuyArea(CenterAreaType areaType)
        {
            int price = GetAreaPrice(areaType);

            if (Save.Center.Bits < price)
                return SystemResult.Fail("Bits insuficientes.");

            Save.Center.Bits -= price;

            return SystemResult.Ok();
        }

        public SystemResult UseMedicinePlayer()
        {
            return UseMedicine(PlayerDigimon);
        }

        public SystemResult UseMedicine(DigimonInstance digimon)
        {
            if (Save.Center.Medicine <= 0)
            {
                return SystemResult.Fail("Você não possui remédios.");
            }

            if (digimon.HealthState != HealthState.Sick)
            {
                return SystemResult.Fail("O Digimon não está doente.");
            }

            Save.Center.Medicine--;

            digimon.Heal(); // ou digimon.HealthState = HealthState.Healthy;

            return SystemResult.Ok("O Digimon foi curado.");
        }

        public void AddDebugDigimon(int id)
        {
            var data = DatabaseManager.Instance.GetDigimon(id);

            if (data == null)
            {
                GD.PrintErr($"Digimon {id} não encontrado.");
                return;
            }

            var digimon = new DigimonInstance(data);

            // Trata como nascimento pra consistência - entra de verdade no Center, então
            // deve sortear passiva igual a um Digimon chocado de ovo.
            digimon.PassiveId = PassiveSystem.RollRandomPassive(data);

            CenterService.AddDigimon(digimon);

            GD.Print($"{data.Name} adicionado ao Center.");
        }

        /// <summary>
        /// Remove um Digimon permanentemente do Center (ex.: pra liberar capacidade em
        /// <see cref="EvolutionBlockedByCapacity"/>). Se ele era o PlayerDigimon selecionado,
        /// passa a seleção pra outro do roster (ou nenhum, se o Center ficou vazio).
        /// </summary>
        public bool DeleteDigimon(DigimonInstance digimon)
        {
            if (digimon == null)
                return false;

            // Nunca deixa o jogador apagar o próprio último Digimon - EnsureCenterCanContinue
            // só cobre o caso de já ter ficado vazio (cria um ovo novo), então é melhor nem
            // deixar chegar nesse estado.
            if (CenterService.GetAllDigimons().Count <= 1)
            {
                GD.Print("Não é possível deletar o único Digimon do Center.");
                return false;
            }

            CenterService.RemoveDigimon(digimon);

            if (PlayerDigimon == digimon)
            {
                PlayerDigimon = CenterService.GetAllDigimons().FirstOrDefault();

                PlayerDigimonChanged?.Invoke();
            }

            DigimonDeleted?.Invoke(digimon);

            return true;
        }

        // --- Sistema de morte (velhice e maus-tratos) ---
        //
        // Diferente de DeleteDigimon (ação manual do jogador, bloqueada se for o último do
        // Center), a morte é permanente e incondicional - inclusive pro último Digimon
        // restante, que aí passa pela mesma rede de segurança de "Center vazio" que já existe
        // pra qualquer outro jeito de zerar o roster (EnsureCenterCanContinue cria um ovo
        // novo). Os contadores puros (DaysSickInARow, ConsecutiveBattleLosses, etc.) moram em
        // DigimonInstance - toda a decisão de "quando isso vira morte e com que chance" fica
        // aqui, centralizada, pra ser fácil de rebalancear sem procurar em vários arquivos.

        private const int SickDaysBeforeDeathRisk = 5;
        private const int SickDeathChancePercent = 20;

        private const int ConsecutiveLossesBeforeDeathRisk = 4;
        private const int ConsecutiveLossDeathChancePercent = 25;

        private const int BattleKnockoutsBeforeDeathRisk = 6;
        private const int BattleKnockoutDeathChancePercent = 15;

        private const int LowHealthBattleEntriesBeforeDeathRisk = 4;
        private const int LowHealthBattleDeathChancePercent = 15;
        private const float LowHealthBattleThreshold = 0.25f;

        private const int DirtyEnvironmentStreakBeforeDeathRisk = 30;
        private const int DirtyEnvironmentDeathChancePercent = 10;

        private bool RollDeathChance(int percentChance) => _random.Next(100) < percentChance;

        /// <summary>
        /// Migração de compatibilidade (ver CenterState.AgeCapMigrationApplied), roda uma
        /// única vez na primeira carga de um save depois do sistema de morte por velhice
        /// existir. Sem isso, qualquer Digimon que já tivesse ultrapassado o teto de idade do
        /// próprio estágio atual (ver DigimonInstance.GetMaxAgeForStage) morreria
        /// instantaneamente, sem aviso nenhum, na primeira virada de dia - em vez disso, dá
        /// uma folga de 1 dia (idade ajustada pro teto menos 1) pra evoluir a tempo.
        /// </summary>
        private void ApplyAgeCapMigration()
        {
            foreach (var digimon in Save.Center.Digimons)
            {
                if (digimon.BaseData == null)
                    continue;

                int maxAge = DigimonInstance.GetMaxAgeForStage(digimon.BaseData.Stage);

                if (digimon.AgeInDays <= maxAge)
                    continue;

                GD.Print(
                    $"Migração de idade: {digimon.DisplayName} estava com {digimon.AgeInDays} " +
                    $"dia(s) (teto do estágio: {maxAge}) - ajustando pra {maxAge - 1}, uma " +
                    "folga de última hora pra evoluir a tempo."
                );

                digimon.AgeInDays = maxAge - 1;
            }
        }

        /// <summary>
        /// Mata um Digimon de vez (velhice ou maus-tratos) - remove do Center, tira da
        /// seleção do jogador se era ele, e avisa tanto quem só precisa limpar o visual
        /// (DigimonDeleted) quanto quem precisa contar pro jogador o que aconteceu
        /// (DigimonDied). Sempre chama EnsureCenterCanContinue depois, já que isso pode
        /// zerar o roster inteiro (diferente de DeleteDigimon, a morte não protege o último
        /// Digimon).
        /// </summary>
        private void KillDigimon(DigimonInstance digimon, string cause)
        {
            GD.Print($"{digimon.DisplayName} morreu: {cause}");

            CenterService.RemoveDigimon(digimon);

            if (PlayerDigimon == digimon)
            {
                PlayerDigimon = CenterService.GetAllDigimons().FirstOrDefault();

                PlayerDigimonChanged?.Invoke();
            }

            DigimonDeleted?.Invoke(digimon);
            DigimonDied?.Invoke(digimon, cause);

            EnsureCenterCanContinue();
        }

        /// <summary>
        /// Checagens de morte que rodam uma vez por dia (ver OnDayPassed): velhice (idade
        /// além do teto do estágio atual - ver DigimonInstance.GetMaxAgeForStage, sempre
        /// certeira) e doença prolongada sem tratamento (chance, não certeza). Retorna true
        /// se o Digimon morreu - quem chamou deve parar de processá-lo (evoluir um Digimon
        /// que acabou de morrer não faz sentido).
        /// </summary>
        private bool EvaluateDailyDeathRisks(DigimonInstance digimon)
        {
            if (digimon.IsPastMaxAge())
            {
                KillDigimon(
                    digimon,
                    $"{digimon.DisplayName} morreu de velhice, sem ter evoluído a tempo."
                );

                return true;
            }

            if (digimon.DaysSickInARow >= SickDaysBeforeDeathRisk &&
                RollDeathChance(SickDeathChancePercent))
            {
                KillDigimon(
                    digimon,
                    $"{digimon.DisplayName} não resistiu depois de {digimon.DaysSickInARow} dias " +
                    "doente sem tratamento."
                );

                return true;
            }

            return false;
        }

        /// <summary>
        /// Checagens de morte por maus-tratos ligadas a batalha - derrotas seguidas,
        /// nocautes acumulados e ter sido escalado fraco demais vezes repetidas. Chamada pra
        /// cada membro do time em ApplyTeamBattleReward, depois dos contadores já terem sido
        /// incrementados. Retorna true se morreu.
        /// </summary>
        private bool EvaluatePostBattleMistreatmentRisk(DigimonInstance member)
        {
            if (member.ConsecutiveBattleLosses >= ConsecutiveLossesBeforeDeathRisk &&
                RollDeathChance(ConsecutiveLossDeathChancePercent))
            {
                KillDigimon(
                    member,
                    $"{member.DisplayName} não aguentou depois de {member.ConsecutiveBattleLosses} " +
                    "derrotas seguidas em batalha."
                );

                return true;
            }

            if (member.TimesKnockedOutInBattle >= BattleKnockoutsBeforeDeathRisk &&
                RollDeathChance(BattleKnockoutDeathChancePercent))
            {
                KillDigimon(
                    member,
                    $"{member.DisplayName} não resistiu depois de ser nocauteado em batalha " +
                    "tantas vezes."
                );

                return true;
            }

            if (member.TimesEnteredBattleAtLowHealth >= LowHealthBattleEntriesBeforeDeathRisk &&
                RollDeathChance(LowHealthBattleDeathChancePercent))
            {
                KillDigimon(
                    member,
                    $"{member.DisplayName} foi mandado pra batalhar debilitado vezes demais e " +
                    "não resistiu."
                );

                return true;
            }

            return false;
        }

        /// <summary>
        /// Checagem de morte por ambiente insalubre - chamada por DigimonWorld.CheckDirtyArea
        /// a cada tick em que o Digimon segue parado numa área suja (ver
        /// DigimonInstance.DirtyEnvironmentStreak).
        /// </summary>
        public void EvaluateDirtyEnvironmentDeathRisk(DigimonInstance digimon)
        {
            if (digimon.DirtyEnvironmentStreak < DirtyEnvironmentStreakBeforeDeathRisk)
                return;

            if (!RollDeathChance(DirtyEnvironmentDeathChancePercent))
                return;

            KillDigimon(
                digimon,
                $"{digimon.DisplayName} morreu depois de tempo demais largado num ambiente " +
                "imundo, cheio de cocô."
            );
        }

        public void EnsureCenterCanContinue()
        {
            if (Save.Center.Digimons.Count == 0 &&
                Save.Center.Eggs.Count == 0)
            {
                GD.Print("Centro vazio. Criando ovo inicial.");

                EggSystem.CreateInitialEgg(CenterService);

                SaveGame();
            }
        }

        public override void _Notification(int what)
        {
            if (what == NotificationWMCloseRequest)
            {
                SaveGame();
            }
        }
    }
}