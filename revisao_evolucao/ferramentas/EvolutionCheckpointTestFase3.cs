using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Save;
using ProjetoDC.Scripts.Systems.Center;
using ProjetoDC.Scripts.Systems.Clock;
using ProjetoDC.Scripts.Systems.Eggs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ProjetoDC.Revisao
{
    /// <summary>
    /// Fase 3 (v2) da revisão de requisitos de evolução: monta cada Digimon PELA CADEIA REAL -
    /// começa no estágio anterior (Lv 1, sem nenhum treino aplicado ainda) e sobe através de
    /// EvolutionSystem.TryToEvolve de verdade, uma aresta por vez (GrowLeg), em vez de criar a
    /// forma final direto com a ficha no Lv 1. É a correção pro erro do modelo apontado pelo
    /// usuário: os stats de entrada de cada forma são o pior caso pela cadeia completa desde o
    /// ovo (rota mais fraca), não a rota mais barata - GrowLeg entra sempre pela rota mais fraca
    /// disponível (a "padrão"/sem treino quando existir) e só treina onde for estritamente
    /// necessário pra alcançar o alvo pedido.
    /// Uso: godot --headless --fixed-fps 60 --path . res://revisao_evolucao/ferramentas/EvolutionCheckpointTestFase3.tscn
    /// </summary>
    public partial class EvolutionCheckpointTestFase3 : Node
    {
        private static readonly BindingFlags NonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private static GameManager Gm => GameManager.Instance;
        private static DatabaseManager Db => DatabaseManager.Instance;

        private int _ok, _fail;

        public override void _Ready() => CallDeferred(nameof(Start));

        private async void Start()
        {
            await Frames(30);

            GD.Print("===== EvolutionCheckpointTestFase3 (v2, cadeia real): início =====");

            CaseA_Guilmon();
            CaseB_Gaomon();
            CaseC_Angewomon();
            CaseD_MachGaogamon();
            CaseE_GigimonNoTie();
            CaseF_NovasFormasPadrao();
            CaseG_PremiumsViaCadeiaMinima();
            CaseH_Chirinmon();
            CaseI_SeisArestasV4();

            GD.Print($"===== EvolutionCheckpointTestFase3: {_ok} ok, {_fail} falha(s) =====");
            GetTree().Quit(_fail > 0 ? 1 : 0);
        }

        private async Task Frames(int n)
        {
            for (int i = 0; i < n; i++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        // ------------------------------------------------------------------ infraestrutura

        private void ResetScratch(int bits = 100_000, int capacity = 500)
        {
            var gm = Gm;
            var t = typeof(GameManager);

            var onHour = (Action)Delegate.CreateDelegate(typeof(Action), gm, t.GetMethod("OnHourPassed", NonPublic));
            var onDay = (Action)Delegate.CreateDelegate(typeof(Action), gm, t.GetMethod("OnDayPassed", NonPublic));
            var onMinute = (Action)Delegate.CreateDelegate(typeof(Action), gm, t.GetMethod("OnMinutePassed", NonPublic));

            if (gm.ClockSystem != null)
            {
                gm.ClockSystem.HourPassed -= onHour;
                gm.ClockSystem.DayPassed -= onDay;
                gm.ClockSystem.MinutePassed -= onMinute;
            }

            var save = new SaveData();

            t.GetProperty("Save").GetSetMethod(true).Invoke(gm, new object[] { save });
            t.GetProperty("CenterService").GetSetMethod(true).Invoke(gm, new object[] { new CenterService(save.Center) });
            var clock = new ClockSystem(save.World);
            t.GetProperty("ClockSystem").GetSetMethod(true).Invoke(gm, new object[] { clock });
            clock.HourPassed += onHour;
            clock.DayPassed += onDay;
            clock.MinutePassed += onMinute;
            gm.EggSystem = new EggSystem();
            t.GetProperty("PlayerDigimon").GetSetMethod(true).Invoke(gm, new object[] { null });
            t.GetField("_hasActiveGame", NonPublic)?.SetValue(gm, false);
            t.GetProperty("ActiveTournament").GetSetMethod(true).Invoke(gm, new object[] { null });
            t.GetProperty("BattleFromExploration").GetSetMethod(true)?.Invoke(gm, new object[] { false });

            save.Center.Bits = bits;
            save.Center.CapacityLimit = capacity;
        }

        private void Check(bool cond, string msg)
        {
            if (cond) { _ok++; GD.Print($"[OK] {msg}"); }
            else { _fail++; GD.PrintErr($"[FALHOU] {msg}"); }
        }

        /// <summary>Cria um Digimon no Lv 1 da espécie pedida, sem NENHUM treino ainda - o ponto
        /// de partida de qualquer cadeia (o "estágio anterior" que o resto do teste evolui a
        /// partir daqui, sempre com EvolutionSystem.TryToEvolve de verdade).</summary>
        private static DigimonInstance SpawnNatural(int speciesId, int level = 1, int age = 0, int happiness = 50, int discipline = 50)
        {
            var species = Db.GetDigimon(speciesId);
            var d = new DigimonInstance(species);

            while (d.Level < level)
                d.GainExperience(d.ExperienceToNextLevel);

            d.AgeInDays = age;
            d.Happiness = happiness;
            d.Discipline = discipline;
            d.RestoreHealth();

            return d;
        }

        private static Func<BaseStats, int> StatReader(TrainingType type) => type switch
        {
            TrainingType.HealthPoints => s => s.HealthPoints,
            TrainingType.Attack => s => s.PhysicalDamage,
            TrainingType.Defense => s => s.PhysicalDefense,
            TrainingType.SpecialAttack => s => s.SpecialDamage,
            TrainingType.SpecialDefense => s => s.SpecialDefense,
            TrainingType.Speed => s => s.Speed,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        /// <summary>Um treino de verdade: TrainingSystem.Execute + DigimonInstance.
        /// ApplyTrainingResult - RNG real (25/50/25%), +10 Felicidade e +5 Disciplina reais, 5 XP
        /// real. Reabastece a stamina direto (100) quando falta - atalho deliberado: regenerar 1
        /// em 1 ponto (72/dia no jogo real) não muda nenhum stat relevante pra decisão de
        /// evolução, só quanto tempo de jogo o treino "gastaria".</summary>
        private void TrainOnce(DigimonInstance d, TrainingType type)
        {
            if (d.Stamina < 10)
                d.Stamina = 100;

            var result = Gm.TrainingSystem.Execute(d, type);

            if (result.Success)
                d.ApplyTrainingResult(result);
        }

        /// <summary>Treina um único stat (real) até ele alcançar um piso, sem checar evolução no
        /// meio - usado pra deixar o stat pronto ANTES de deixar o Digimon subir de nível/idade,
        /// senão a subida (que dispara checagem de verdade) pode evoluir cedo demais com o stat
        /// só pela metade.</summary>
        private void TrainUntilStat(DigimonInstance d, TrainingType type, Func<BaseStats, int> read, int floor, int maxAttempts = 150)
        {
            for (int i = 0; i < maxAttempts && read(d.CurrentStats) < floor; i++)
                TrainOnce(d, type);
        }

        /// <summary>Checkpoint real (SEM XP): fim de uma batalha 3x3 vencida fora de torneio, com
        /// time inimigo vazio de propósito - BattleRewardCalculator devolve XP=0 pra time vazio,
        /// então "if (xpGained > 0) GainExperience(...)" nunca roda; RecordBattleResult ainda
        /// incrementa BattlesFought de verdade, e TryToEvolve roda normalmente.</summary>
        private void BattleCheckpoint(DigimonInstance member)
        {
            var t = typeof(GameManager);

            t.GetProperty("PlayerBattleTeam").GetSetMethod(true)
                .Invoke(Gm, new object[] { new List<DigimonInstance> { member } });
            t.GetProperty("EnemyBattleTeam").GetSetMethod(true)
                .Invoke(Gm, new object[] { new List<DigimonInstance>() });

            Gm.ApplyTeamBattleReward(BattleResult.PlayerWon);
        }

        /// <summary>Checkpoint real (COM XP): mesma função, mas com um inimigo de verdade -
        /// concede XP real (GainExperience -> LevelUp real) e ainda assim checa evolução no
        /// fim, exatamente como GameManager.ApplyTeamBattleReward faz de verdade.</summary>
        private void BattleCheckpointWithLevelUp(DigimonInstance member)
        {
            var t = typeof(GameManager);

            var enemySpecies = Db.GetAllDigimons().First(s => s.Stage == DigimonStage.Mega);
            var enemy = new DigimonInstance(enemySpecies) { Level = member.Level + 10 };

            t.GetProperty("PlayerBattleTeam").GetSetMethod(true)
                .Invoke(Gm, new object[] { new List<DigimonInstance> { member } });
            t.GetProperty("EnemyBattleTeam").GetSetMethod(true)
                .Invoke(Gm, new object[] { new List<DigimonInstance> { enemy } });

            Gm.ApplyTeamBattleReward(BattleResult.PlayerWon);
        }

        /// <summary>Mesmo checkpoint real (COM XP), mas com um inimigo Rookie no mesmo nível (em
        /// vez do Mega+10 de <see cref="BattleCheckpointWithLevelUp"/>) - XP bem menor por
        /// batalha, sobe no máximo ~1 nível por vez. Usado só quando o salto grande da versão
        /// normal pula direto por cima de uma janela estreita entre duas irmãs (ex.: Caso I3,
        /// Growlmon -> WarGrowlmon/MetalGreymon só ~14 PA / 23 HP de diferença) e a checagem de
        /// verdade (que roda uma vez por chamada, depois de aplicar TODO o XP daquela chamada)
        /// nunca via o nível exato em que só a forma-alvo já era válida.</summary>
        private void SlowBattleCheckpoint(DigimonInstance member)
        {
            var t = typeof(GameManager);

            var enemySpecies = Db.GetAllDigimons().First(s => s.Stage == DigimonStage.Rookie);
            var enemy = new DigimonInstance(enemySpecies) { Level = member.Level };

            t.GetProperty("PlayerBattleTeam").GetSetMethod(true)
                .Invoke(Gm, new object[] { new List<DigimonInstance> { member } });
            t.GetProperty("EnemyBattleTeam").GetSetMethod(true)
                .Invoke(Gm, new object[] { new List<DigimonInstance> { enemy } });

            Gm.ApplyTeamBattleReward(BattleResult.PlayerWon);
        }

        /// <summary>Checkpoint real: virada de dia (ClockSystem.DayPassed -> OnDayPassed). Cura
        /// doença na hora (Heal() real) - sem isso, TryBecomeSick (aleatório, todo dia) podia
        /// deixar o Digimon doente vários dias seguidos e arriscar uma morte por negligência
        /// (20%/dia após 5 dias doente seguidos) - um risco real do jogo, mas não o que este
        /// teste quer medir (só os requisitos de evolução).</summary>
        private void DayPassedCheckpoint(DigimonInstance member)
        {
            if (!Gm.Save.Center.Digimons.Contains(member))
                Gm.CenterService.AddDigimon(member);

            // Alimenta antes de passar o dia: ClockSystem.Update dispara HourPassed de verdade
            // pra cada hora (24x aqui), e cada uma delas chama AdvanceHour -> ConsumeHunger; sem
            // comida a Fome cai a zero em poucos dias e "if (Hunger <= 20) ChangeHappiness(-1)"
            // drena a Felicidade pra 0 depois de o suficientes viradas de dia (foi exatamente
            // isso que derrubou o Caso H1 - Sleipmon exige Felic>=40 e a Felicidade tinha ido a
            // zero só de tantos dias passados pra subir de nível, não por causa de nenhum
            // requisito de evolução em si). Feed() é a função real (mesma que o Center usa).
            member.Feed(member.MaxHunger);

            Gm.ClockSystem.Update(1440.0);

            if (member.HealthState == HealthState.Sick)
                member.Heal();
        }

        /// <summary>Percorre UMA aresta da cadeia evolutiva a partir do estado atual de <paramref
        /// name="d"/>: treina (de verdade) o stat-assinatura até o piso pedido - se houver -
        /// ANTES de deixar subir de nível/idade (mesma ordem já validada: treinar primeiro evita
        /// que a subida de nível/idade dispare a checagem com o stat pela metade), depois sobe de
        /// nível por batalha (com XP real) até o nível exigido e, por fim, avança dias até a
        /// idade exigida - checando evolução (via os checkpoints reais) a cada passo o tempo
        /// todo. Para quando evolui (BaseData.Id muda) ou quando os requisitos batem.</summary>
        private void GrowLeg(DigimonInstance d, int requiredLevel, int requiredAge,
            TrainingType? stat = null, int floor = 0, int maxBattles = 90, int maxDays = 60,
            int requiredBattles = 0, float requiredWinRate = 0f)
        {
            GrowLeg(d, requiredLevel, requiredAge,
                stat.HasValue ? new[] { (stat.Value, floor) } : Array.Empty<(TrainingType, int)>(),
                maxBattles, maxDays, requiredBattles, requiredWinRate);
        }

        /// <summary>Mesma coisa que a sobrecarga acima, mas treinando VÁRIOS stats (todos de
        /// verdade, um de cada vez até o piso) antes de deixar subir de nível/idade - usado
        /// quando a aresta em si (não uma "assinatura" limpa de um só stat) pede mais de um stat
        /// ao mesmo tempo pra ficar alcançável. <paramref name="requiredBattles"/>/<paramref
        /// name="requiredWinRate"/> continuam a batalha mesmo depois do Nível bater, pra arestas
        /// que também exigem um mínimo de RequiredBattles/RequiredWinRate (achado na Fase 6 -
        /// Yuramon -> Budmon pede 8 batalhas, e o Nível 2 sozinho não gera batalha suficiente).</summary>
        private void GrowLeg(DigimonInstance d, int requiredLevel, int requiredAge,
            (TrainingType stat, int floor)[] stats, int maxBattles = 90, int maxDays = 60,
            int requiredBattles = 0, float requiredWinRate = 0f)
        {
            int startId = d.BaseData.Id;

            foreach (var (stat, floor) in stats)
                TrainUntilStat(d, stat, StatReader(stat), floor);

            for (int i = 0; i < maxBattles && d.BaseData.Id == startId &&
                 (d.Level < requiredLevel || d.BattlesFought < requiredBattles ||
                  (requiredWinRate > 0 && d.WinRate < requiredWinRate)); i++)
                BattleCheckpointWithLevelUp(d);

            for (int i = 0; i < maxDays && d.AgeInDays < requiredAge && d.BaseData.Id == startId; i++)
                DayPassedCheckpoint(d);
        }

        /// <summary>Mesma coisa que a sobrecarga multi-stat de <see cref="GrowLeg"/>, mas subindo
        /// de nível com <see cref="SlowBattleCheckpoint"/> (granularidade fina) em vez de
        /// <see cref="BattleCheckpointWithLevelUp"/> - ver o comentário de SlowBattleCheckpoint.</summary>
        private void GrowLegPrecise(DigimonInstance d, int requiredLevel, int requiredAge,
            (TrainingType stat, int floor)[] stats, int maxBattles = 400, int maxDays = 60)
        {
            int startId = d.BaseData.Id;

            foreach (var (stat, floor) in stats)
                TrainUntilStat(d, stat, StatReader(stat), floor);

            for (int i = 0; i < maxBattles && d.Level < requiredLevel && d.BaseData.Id == startId; i++)
                SlowBattleCheckpoint(d);

            for (int i = 0; i < maxDays && d.AgeInDays < requiredAge && d.BaseData.Id == startId; i++)
                DayPassedCheckpoint(d);
        }

        // ------------------------------------------------------------------ caso a) Guilmon

        private void CaseA_Guilmon()
        {
            GD.Print("\n--- Caso A: Guilmon (id 7) - já é o estágio sendo testado, Lv 1 direto ---");

            const int guilmonId = 7;

            ResetScratch();

            var pa = SpawnNatural(guilmonId);

            GrowLeg(pa, requiredLevel: 12, requiredAge: 2, stat: TrainingType.Attack, floor: 104);

            Check(pa.BaseData.Name == "Growlmon",
                $"A1 (só PA, treino real): resultado {pa.BaseData.Name} (esperado Growlmon)");

            ResetScratch();

            var none = SpawnNatural(guilmonId);

            GrowLeg(none, requiredLevel: 12, requiredAge: 13);

            Check(none.BaseData.Name == "Dimetromon",
                $"A2 (sem treino, até a Idade 13): resultado {none.BaseData.Name} (esperado Dimetromon - forma padrão)");
        }

        // ------------------------------------------------------------------ caso b) Gaomon

        private void CaseB_Gaomon()
        {
            GD.Print("\n--- Caso B: Gaomon (id 5) - já é o estágio sendo testado, Lv 1 direto ---");

            const int gaomonId = 5;

            ResetScratch();

            var vel = SpawnNatural(gaomonId, age: 5); // Gladimon exige Idade >= 5 (as outras duas só >= 2)

            GrowLeg(vel, requiredLevel: 18, requiredAge: 5, stat: TrainingType.Speed, floor: 82);

            Check(vel.BaseData.Name == "Gladimon",
                $"B1 (só Vel até o Lv 18, treino real): resultado {vel.BaseData.Name} (esperado Gladimon)");

            ResetScratch();

            var pa = SpawnNatural(gaomonId);

            GrowLeg(pa, requiredLevel: 12, requiredAge: 2, stat: TrainingType.Attack, floor: 108);

            Check(pa.BaseData.Name == "Gaogamon",
                $"B2 (só PA, treino real): resultado {pa.BaseData.Name} (esperado Gaogamon)");

            ResetScratch();

            var none = SpawnNatural(gaomonId);

            GrowLeg(none, requiredLevel: 12, requiredAge: 13);

            Check(none.BaseData.Name == "Garurumon",
                $"B3 (sem treino, até a Idade 13): resultado {none.BaseData.Name} (esperado Garurumon - forma padrão)");
        }

        // ------------------------------------------------------------------ caso c) Angewomon

        private void CaseC_Angewomon()
        {
            GD.Print("\n--- Caso C: Angewomon (id 72), pela cadeia real a partir de Angemon (id 16) ---");

            const int angemonId = 16;

            // Angemon -> Angewomon é a rota PADRÃO do próprio Angemon (Lv20, Idade 24, sem
            // treino nenhum) - é a "cadeia mínima" pra chegar em Angewomon.
            DigimonInstance ChegarEmAngewomon(int happiness = 50)
            {
                var d = SpawnNatural(angemonId, happiness: happiness);

                GrowLeg(d, requiredLevel: 20, requiredAge: 24);

                return d;
            }

            foreach (int happiness in new[] { 30, 50 })
            {
                ResetScratch();

                var ea = ChegarEmAngewomon(happiness);

                Check(ea.BaseData.Name == "Angewomon", $"C0 ({happiness}): chegou em Angewomon vindo de Angemon (pré-condição) - {ea.BaseData.Name}");

                GrowLeg(ea, requiredLevel: 28, requiredAge: 8, stat: TrainingType.SpecialAttack, floor: 213);

                Check(ea.BaseData.Name == "Ophanimon",
                    $"C1 (Angemon -> Angewomon padrão -> só EA, Felicidade={happiness}): resultado {ea.BaseData.Name} (esperado Ophanimon)");
            }

            ResetScratch();

            var ed = ChegarEmAngewomon();

            GrowLeg(ed, requiredLevel: 28, requiredAge: 8, stat: TrainingType.SpecialDefense, floor: 108);

            Check(ed.BaseData.Name == "Lilithmon",
                $"C2 (Angemon -> Angewomon padrão -> só ED): resultado {ed.BaseData.Name} (esperado Lilithmon)");

            ResetScratch();

            var none = ChegarEmAngewomon();

            GrowLeg(none, requiredLevel: 28, requiredAge: 50);

            Check(none.BaseData.Name == "Magnadramon",
                $"C3 (Angemon -> Angewomon padrão -> sem treino, até a Idade 50): resultado {none.BaseData.Name} (esperado Magnadramon - forma padrão)");
        }

        // ------------------------------------------------------------------ caso d) MachGaogamon

        private void CaseD_MachGaogamon()
        {
            GD.Print("\n--- Caso D: MachGaogamon (id 48), pela cadeia real a partir de Kotemon -> Gaogamon ---");

            const int kotemonId = 145;

            // "MachGaogamon vindo de Gaogamon": Gaogamon (Champion) sozinho, criado do zero no
            // Lv 1, não chega nem perto de Gaogamon->MachGaogamon. Um Gaogamon de verdade só
            // existe por ter evoluído de algo - testei primeiro a rota Gaomon (assinatura PA,
            // Caso B) mas ela não dá HP suficiente pro próximo passo (ver relatório); a rota mais
            // forte é Kotemon (HP+PA+PD, sem "assinatura" de um stat só) - depois disso, "sem
            // treinar" vale a partir daqui. NOTA (Fase 3 v3): essa aresta era o achado original do
            // usuário (carry-over pela rota Kotemon fazia WereGarurumon atropelar MachGaogamon) -
            // o CSV v4 corrigiu subindo a Idade de MachGaogamon de 5 pra 24 (ver Caso I1, que
            // cobre as duas rotas de entrada - Gaomon e Kotemon - lado a lado).
            DigimonInstance ChegarEmGaogamon()
            {
                var d = SpawnNatural(kotemonId);

                // PD não entra no treino: o natural (30 base + 2/nível) já cobre os 50 exigidos
                // por Kotemon->Gaogamon sozinho - treinar mais que isso deixaria o PD alto o
                // bastante pra WereGarurumon (a outra saída de Gaogamon, PD>=80) competir e
                // vencer por Score antes de MachGaogamon (PD não é exigido) ficar pronto.
                GrowLeg(d, requiredLevel: 12, requiredAge: 2,
                    new (TrainingType, int)[] { (TrainingType.HealthPoints, 170), (TrainingType.Attack, 90) });

                return d;
            }

            ResetScratch();

            var d = ChegarEmGaogamon();

            Check(d.BaseData.Name == "Gaogamon", $"D-1: chegou em Gaogamon vindo de Kotemon (HP+PA+PD) - {d.BaseData.Name}");
            GD.Print($"  [diagnóstico D-1] Lv{d.Level} Age{d.AgeInDays} HP{d.CurrentStats.HealthPoints} PA{d.CurrentStats.PhysicalDamage} PD{d.CurrentStats.PhysicalDefense} Vel{d.CurrentStats.Speed}");

            GrowLeg(d, requiredLevel: 20, requiredAge: 24, maxBattles: 150);

            GD.Print($"  [diagnóstico D0] Lv{d.Level} Age{d.AgeInDays} HP{d.CurrentStats.HealthPoints} PA{d.CurrentStats.PhysicalDamage} PD{d.CurrentStats.PhysicalDefense} Vel{d.CurrentStats.Speed} nome={d.BaseData.Name}");
            Check(d.BaseData.Name == "MachGaogamon", $"D0: chegou em MachGaogamon vindo de Gaogamon sem treino - {d.BaseData.Name}");

            GrowLeg(d, requiredLevel: 28, requiredAge: 50);

            Check(d.BaseData.Name == "Gankoomon",
                $"D1 (Gaomon -> Gaogamon -> MachGaogamon -> sem treino, até a Idade 50): resultado {d.BaseData.Name} (esperado Gankoomon - forma padrão)");

            // Felicidade baixa forçada na hora da checagem final (independe do que o treino real
            // faria com ela, já que aqui não há treino nenhum depois de Gaogamon) - MirageGaogamon
            // exige Felic>=40, então com Felicidade baixa quem vence continua sendo o padrão
            // Gankoomon.
            ResetScratch();

            var lowHappy = ChegarEmGaogamon();

            GrowLeg(lowHappy, requiredLevel: 20, requiredAge: 24);
            GrowLeg(lowHappy, requiredLevel: 28, requiredAge: 50 - 1); // fica 1 dia aquém de propósito
            lowHappy.Happiness = 30;
            DayPassedCheckpoint(lowHappy); // completa a Idade 50 já com a Felicidade baixa

            Check(lowHappy.BaseData.Name == "Gankoomon",
                $"D2 (mesma cadeia, Felicidade forçada em 30 na checagem final): resultado {lowHappy.BaseData.Name} (esperado Gankoomon)");
        }

        // ------------------------------------------------------------------ caso e) Gigimon

        /// <summary>Fase 6: Gigimon (InTraining) entrou nos 189 ligações revisadas - o empate de
        /// Score original (o motivo do nome antigo "NoTie") não existe mais, Elizamon e Dorumon
        /// agora assinam por ED/EA como qualquer outro par do Caso I8. Reescrito no mesmo formato.</summary>
        private void CaseE_GigimonNoTie()
        {
            GD.Print("\n--- Caso E: Gigimon (InTraining) -> Guilmon (padrão) / Elizamon (ED) / Dorumon (EA+8 batalhas) ---");

            const int gigimonId = 33;

            ResetScratch();
            var guilmon = SpawnNatural(gigimonId);
            GrowLeg(guilmon, requiredLevel: 2, requiredAge: 5);
            Check(guilmon.BaseData.Name == "Guilmon", $"E1 (Gigimon -> sem treino, até a Idade 5): resultado {guilmon.BaseData.Name} (esperado Guilmon - forma padrão)");

            ResetScratch();
            var elizamon = SpawnNatural(gigimonId);
            // Elizamon também exige EA>=6 (a ficha base de Gigimon só tem EA4) e Disciplina<=45 -
            // treino de verdade sobe Disciplina +5 a cada sucesso (mesmo bug encontrado na
            // ferramenta da Fase 4), então a Disciplina só pode ser ajustada DEPOIS do treino.
            TrainUntilStat(elizamon, TrainingType.SpecialAttack, s => s.SpecialDamage, 6);
            TrainUntilStat(elizamon, TrainingType.SpecialDefense, s => s.SpecialDefense, 13);
            elizamon.Discipline = 30;
            BattleCheckpointWithLevelUp(elizamon);
            Check(elizamon.BaseData.Name == "Elizamon", $"E2 (Gigimon -> EA até 6 + ED até 13, Disciplina<=45): resultado {elizamon.BaseData.Name} (esperado Elizamon)");

            ResetScratch();
            var dorumon = SpawnNatural(gigimonId);
            GrowLeg(dorumon, requiredLevel: 2, requiredAge: 0, stat: TrainingType.SpecialAttack, floor: 13, requiredBattles: 8);
            Check(dorumon.BaseData.Name == "Dorumon", $"E3 (Gigimon -> EA até 13 + 8 batalhas): resultado {dorumon.BaseData.Name} (esperado Dorumon)");
        }

        // ------------------------------------------------------------------ caso f) novas formas padrão (InTraining->Rookie / Rookie->Champion)

        private void CaseF_NovasFormasPadrao()
        {
            GD.Print("\n--- Caso F: cadeias que ganharam forma padrão nesta revisão ---");

            // Rookies que só tinham saída premium (já são o próprio estágio testado, Lv 1 direto).
            CheckDefaultAtAge(132, "Dracomon", "Coredramon (Green)", requiredLevel: 12, requiredAge: 13);
            CheckDefaultAtAge(139, "Veemon", "Veedramon", requiredLevel: 12, requiredAge: 13);
            CheckDefaultAtAge(149, "Dorumon", "Dorugamon", requiredLevel: 12, requiredAge: 13);

            // Hackmon vindo de Tsumemon (InTraining, id 115): a aresta Tsumemon->Hackmon não foi
            // tocada pela revisão (não tem "assinatura" de um stat só - pede EA+ED+Vel juntos,
            // acima do que o nível puro dá) - treina os três só pra CHEGAR em Hackmon, e dali em
            // diante, sem treino nenhum, até BaoHackmon (padrão).
            ResetScratch();

            var h = SpawnNatural(115);

            TrainUntilStat(h, TrainingType.SpecialAttack, s => s.SpecialDamage, 30);
            TrainUntilStat(h, TrainingType.SpecialDefense, s => s.SpecialDefense, 18);
            TrainUntilStat(h, TrainingType.Speed, s => s.Speed, 29);
            GD.Print($"  [diagnóstico F0 pré-nível] Lv{h.Level} HP{h.CurrentStats.HealthPoints} EA{h.CurrentStats.SpecialDamage} ED{h.CurrentStats.SpecialDefense} Vel{h.CurrentStats.Speed}");

            // Hackmon exige Idade >= 1 e BEMmon só Idade >= 0 - se a Idade não avançar ANTES da
            // subida de nível, o primeiro nível já vence com BEMmon (score mais baixo, mas o
            // único válido no instante em que a checagem roda, já que Hackmon fica de fora só
            // pela Idade). Avança 1 dia primeiro (sem XP, Level ainda não sobe com isso) pra
            // colocar as duas em pé de igualdade e deixar o Score decidir.
            DayPassedCheckpoint(h);

            GrowLeg(h, requiredLevel: 3, requiredAge: 1);

            GD.Print($"  [diagnóstico F0 pós] Lv{h.Level} Age{h.AgeInDays} HP{h.CurrentStats.HealthPoints} EA{h.CurrentStats.SpecialDamage} ED{h.CurrentStats.SpecialDefense} Vel{h.CurrentStats.Speed} nome={h.BaseData.Name}");

            Check(h.BaseData.Name == "Hackmon", $"F0: chegou em Hackmon vindo de Tsumemon (EA+ED+Vel) - {h.BaseData.Name}");

            GrowLeg(h, requiredLevel: 12, requiredAge: 13);

            Check(h.BaseData.Name == "BaoHackmon",
                $"F (Tsumemon -> Hackmon -> sem treino, até a Idade 13): resultado {h.BaseData.Name} (esperado BaoHackmon - forma padrão)");
        }

        private void CheckDefaultAtAge(int speciesId, string speciesName, string expectedForm, int requiredLevel, int requiredAge)
        {
            ResetScratch();

            var d = SpawnNatural(speciesId);

            GrowLeg(d, requiredLevel, requiredAge);

            Check(d.BaseData.Name == expectedForm,
                $"F ({speciesName}, sem treino, até a Idade {requiredAge}): resultado {d.BaseData.Name} (esperado {expectedForm} - forma padrão)");
        }

        // ------------------------------------------------------------------ caso g) premiums Ultimate->Mega via cadeia mínima

        private void CaseG_PremiumsViaCadeiaMinima()
        {
            GD.Print("\n--- Caso G: Grademon/SaviorHackmon/Wingdramon pela rota mais fraca (Champion padrão) ---");

            // Grademon: rota mais fraca é Raptordramon, mas um Raptordramon de verdade só existe
            // por ter evoluído (assinatura PA) do Dorumon - criado do zero no Lv 1 ele não tem
            // stat suficiente nem pra completar essa própria aresta.
            TestPremiumELine(
                fromSpeciesId: 153, fromName: "Raptordramon",
                toPadraoLevel: 20, toPadraoAge: 24, targetName: "Grademon",
                premiumStat: TrainingType.Speed, premiumFloor: 192, premiumLevel: 36, premiumAge: 12, premiumName: "Alphamon",
                defaultLevel: 28, defaultAge: 50, defaultName: "Dynasmon",
                preFromSpeciesId: 149, preFromName: "Dorumon", preStat: TrainingType.Attack, preFloor: 120, preLevel: 18, preAge: 5);

            // SaviorHackmon: rota mais fraca é BaoHackmon (padrão, Lv20/Idade24) - aqui um
            // BaoHackmon criado do zero no Lv 1 já é suficiente (testado, sem precisar voltar
            // até o Hackmon).
            TestPremiumELine(
                fromSpeciesId: 164, fromName: "BaoHackmon",
                toPadraoLevel: 20, toPadraoAge: 24, targetName: "SaviorHackmon",
                premiumStat: TrainingType.Speed, premiumFloor: 185, premiumLevel: 36, premiumAge: 12, premiumName: "Jesmon",
                defaultLevel: 28, defaultAge: 50, defaultName: "Gankoomon");

            // Wingdramon: rota mais fraca é Coredramon (Blue), mas ele só existe de verdade por
            // ter evoluído (assinatura Vel) do Dracomon.
            TestPremiumELine(
                fromSpeciesId: 136, fromName: "Coredramon (Blue)",
                toPadraoLevel: 20, toPadraoAge: 24, targetName: "Wingdramon",
                premiumStat: TrainingType.Attack, premiumFloor: 235, premiumLevel: 36, premiumAge: 12, premiumName: "SlayerDramon",
                defaultLevel: 28, defaultAge: 50, defaultName: "Dynasmon",
                preFromSpeciesId: 132, preFromName: "Dracomon", preStat: TrainingType.Speed, preFloor: 108, preLevel: 18, preAge: 5);
        }

        /// <summary>Padrão comum aos 3 casos do G: chega no Ultimate-alvo pela rota Champion mais
        /// fraca (padrão, sem treino) - opcionalmente voltando mais um estágio (Rookie), se o
        /// Champion em si só existir de verdade por ter evoluído pela própria assinatura -,
        /// depois testa separadamente (1) treinar só a assinatura da premium até o Lv da premium -
        /// confirma que ela vence e que o padrão Mega (que exige Idade bem maior) não dispara
        /// antes - e (2) sem treino nenhum, até a Idade da forma padrão Mega.</summary>
        private void TestPremiumELine(
            int fromSpeciesId, string fromName, int toPadraoLevel, int toPadraoAge, string targetName,
            TrainingType premiumStat, int premiumFloor, int premiumLevel, int premiumAge, string premiumName,
            int defaultLevel, int defaultAge, string defaultName,
            int? preFromSpeciesId = null, string preFromName = null, TrainingType preStat = default, int preFloor = 0, int preLevel = 0, int preAge = 0)
        {
            DigimonInstance ChegarNoChampion()
            {
                if (!preFromSpeciesId.HasValue)
                    return SpawnNatural(fromSpeciesId);

                var pre = SpawnNatural(preFromSpeciesId.Value);

                GrowLeg(pre, preLevel, preAge, preStat, preFloor);

                Check(pre.BaseData.Name == fromName, $"G-1 ({fromName}): chegou vindo de {preFromName} (só {preStat}) - {pre.BaseData.Name}");

                return pre;
            }

            // (1) só a assinatura da premium, até o Lv da premium.
            ResetScratch();

            var premium = ChegarNoChampion();

            GrowLeg(premium, toPadraoLevel, toPadraoAge);

            Check(premium.BaseData.Name == targetName, $"G0 ({targetName}): chegou vindo de {fromName} sem treino - {premium.BaseData.Name}");

            GrowLeg(premium, premiumLevel, premiumAge, premiumStat, premiumFloor, maxBattles: 260);

            GD.Print($"  [diagnóstico G1 {targetName}->{premiumName}] Lv{premium.Level} Age{premium.AgeInDays} HP{premium.CurrentStats.HealthPoints} PA{premium.CurrentStats.PhysicalDamage} PD{premium.CurrentStats.PhysicalDefense} Vel{premium.CurrentStats.Speed} nome={premium.BaseData.Name}");

            Check(premium.BaseData.Name == premiumName,
                $"G1 ({fromName} -> {targetName} -> só {premiumStat}, até o Lv {premiumLevel}): resultado {premium.BaseData.Name} " +
                $"(esperado {premiumName}, sem {defaultName} disparar antes - Idade ficou em {premium.AgeInDays}, bem abaixo dos {defaultAge} que {defaultName} exige)");

            // (2) sem treino nenhum, até a Idade da forma padrão Mega.
            ResetScratch();

            var none = ChegarNoChampion();

            GrowLeg(none, toPadraoLevel, toPadraoAge);

            Check(none.BaseData.Name == targetName, $"G0 ({targetName}, 2ª cadeia): chegou vindo de {fromName} sem treino - {none.BaseData.Name}");

            GrowLeg(none, defaultLevel, defaultAge);

            Check(none.BaseData.Name == defaultName,
                $"G2 ({fromName} -> {targetName} -> sem treino, até a Idade {defaultAge}): resultado {none.BaseData.Name} (esperado {defaultName} - forma padrão)");
        }

        // ------------------------------------------------------------------ caso h) Chirinmon

        private void CaseH_Chirinmon()
        {
            GD.Print("\n--- Caso H: Chirinmon (id 131), pela cadeia real a partir de Reppamon (id 130) ---");

            const int reppamonId = 130;

            // Diferente de Grademon/SaviorHackmon/Wingdramon: Reppamon NÃO tem rota padrão pra
            // Chirinmon (a própria Reppamon->Chirinmon é a rota "EA", já que a rota padrão de
            // Reppamon vai pra Taomon) - chegar em Chirinmon exige treinar EA de qualquer forma.
            DigimonInstance ChegarEmChirinmon()
            {
                var d = SpawnNatural(reppamonId);

                GrowLeg(d, requiredLevel: 28, requiredAge: 8, stat: TrainingType.SpecialAttack, floor: 243);

                return d;
            }

            ResetScratch();

            var premium = ChegarEmChirinmon();

            Check(premium.BaseData.Name == "Chirinmon", $"H0: chegou em Chirinmon vindo de Reppamon (via EA) - {premium.BaseData.Name}");

            // Sleipmon pede PA (assinatura) E Vel (base compartilhada com Kentaurusmon/Sleipmon) -
            // o Vel natural da cadeia (Reppamon treinado só em EA) não chega lá sozinho.
            GrowLeg(premium, requiredLevel: 36, requiredAge: 12,
                new (TrainingType, int)[] { (TrainingType.Attack, 265), (TrainingType.Speed, 150) }, maxBattles: 260);

            GD.Print($"  [diagnóstico H1] Lv{premium.Level} Age{premium.AgeInDays} HP{premium.CurrentStats.HealthPoints} PA{premium.CurrentStats.PhysicalDamage} PD{premium.CurrentStats.PhysicalDefense} EA{premium.CurrentStats.SpecialDamage} Vel{premium.CurrentStats.Speed} Happ{premium.Happiness} Disc{premium.Discipline} nome={premium.BaseData.Name}");

            Check(premium.BaseData.Name == "Sleipmon",
                $"H1 (Reppamon -> Chirinmon -> só PA, até o Lv 36): resultado {premium.BaseData.Name} " +
                $"(esperado Sleipmon, sem Kentaurusmon disparar antes - Idade ficou em {premium.AgeInDays}, bem abaixo dos 50 que Kentaurusmon exige)");

            ResetScratch();

            var none = ChegarEmChirinmon();

            Check(none.BaseData.Name == "Chirinmon", $"H0 (2ª cadeia): chegou em Chirinmon vindo de Reppamon (via EA) - {none.BaseData.Name}");

            GrowLeg(none, requiredLevel: 28, requiredAge: 50);

            Check(none.BaseData.Name == "Kentaurusmon",
                $"H2 (Reppamon -> Chirinmon -> sem mais treino, até a Idade 50): resultado {none.BaseData.Name} (esperado Kentaurusmon - forma padrão)");
        }

        // ------------------------------------------------------------------ caso i) Fase 3 v3 - as 6 arestas corrigidas pelo CSV v4

        /// <summary>Fase 3 v3: o achado do usuário era uma classe de problema que a Fase 3 v2 não
        /// enxergava - a checagem F do verificador (rota por rota, sem misturar stats de rotas
        /// diferentes). Gaogamon->MachGaogamon e mais 5 pares tinham o mesmo defeito: quem chega
        /// pela rota FORTE já carrega PD/PA/EA/ED altos o bastante pra validar a irmã de Score
        /// maior junto, então a forma "padrão" (calibrada pela rota fraca) nunca dispara por essa
        /// rota. O CSV v4 corrige subindo a idade/stat da forma canônica e movendo a assinatura da
        /// irmã pra um stat que NENHUMA rota de entrada já traz alto. Cada caso abaixo testa as
        /// DUAS rotas de entrada (quando existem) pra confirmar que a forma "padrão" agora dispara
        /// mesmo vindo pela rota mais forte, e que a assinatura ainda funciona treinando só ela.</summary>
        private void CaseI_SeisArestasV4()
        {
            GD.Print("\n--- Caso I: Fase 3 v3 - as 6 arestas corrigidas pelo CSV v4 (carry-over rota-a-rota) ---");

            CaseI1_Gaogamon();
            CaseI2_Togemon();
            CaseI3_WarGrowlmon();
            CaseI4_JewelBeemon();
            CaseI5_Angewomon();
            CaseI6_Knightmon();
            CaseI7_GrowlmonMetalGreymon();
            CaseI8_Fase6BabyInTraining();
        }

        private void CaseI1_Gaogamon()
        {
            GD.Print("\n--- Caso I1: Gaogamon -> WereGarurumon / MachGaogamon, via Gaomon e via Kotemon ---");

            const int gaomonId = 5;
            const int kotemonId = 145;

            DigimonInstance ViaGaomon()
            {
                var d = SpawnNatural(gaomonId);
                GrowLeg(d, requiredLevel: 12, requiredAge: 2, stat: TrainingType.Attack, floor: 108);
                return d;
            }

            DigimonInstance ViaKotemon()
            {
                var d = SpawnNatural(kotemonId);
                GrowLeg(d, requiredLevel: 12, requiredAge: 2,
                    new (TrainingType, int)[] { (TrainingType.HealthPoints, 170), (TrainingType.Attack, 90) });
                return d;
            }

            foreach (var (rota, chegar) in new (string, Func<DigimonInstance>)[] { ("Gaomon", ViaGaomon), ("Kotemon", ViaKotemon) })
            {
                ResetScratch();
                var were = chegar();
                Check(were.BaseData.Name == "Gaogamon", $"I1-{rota}: chegou em Gaogamon - {were.BaseData.Name}");

                GrowLeg(were, requiredLevel: 20, requiredAge: 5, stat: TrainingType.SpecialAttack, floor: 47);

                Check(were.BaseData.Name == "WereGarurumon",
                    $"I1a ({rota} -> Gaogamon -> só EA até 47, Lv 20): resultado {were.BaseData.Name} (esperado WereGarurumon)");

                ResetScratch();
                var mach = chegar();
                GrowLeg(mach, requiredLevel: 20, requiredAge: 24);

                Check(mach.BaseData.Name == "MachGaogamon",
                    $"I1b ({rota} -> Gaogamon -> sem treino, até a Idade 24): resultado {mach.BaseData.Name} (esperado MachGaogamon - forma padrão)");
            }
        }

        private void CaseI2_Togemon()
        {
            GD.Print("\n--- Caso I2: Togemon -> Lilamon / Lillymon, via Palmon ---");

            const int palmonId = 6;

            DigimonInstance ChegarEmTogemon()
            {
                var d = SpawnNatural(palmonId);
                GrowLeg(d, requiredLevel: 12, requiredAge: 2,
                    new (TrainingType, int)[] { (TrainingType.HealthPoints, 136), (TrainingType.Attack, 67), (TrainingType.Speed, 51) });
                return d;
            }

            ResetScratch();
            var lila = ChegarEmTogemon();
            Check(lila.BaseData.Name == "Togemon", $"I2-0: chegou em Togemon vindo de Palmon - {lila.BaseData.Name}");

            GrowLeg(lila, requiredLevel: 20, requiredAge: 5, stat: TrainingType.SpecialDefense, floor: 62);

            Check(lila.BaseData.Name == "Lilamon",
                $"I2a (Palmon -> Togemon -> só ED até 62, Lv 20): resultado {lila.BaseData.Name} (esperado Lilamon)");

            ResetScratch();
            var lily = ChegarEmTogemon();
            GrowLeg(lily, requiredLevel: 20, requiredAge: 24);

            Check(lily.BaseData.Name == "Lillymon",
                $"I2b (Palmon -> Togemon -> sem treino, até a Idade 24): resultado {lily.BaseData.Name} (esperado Lillymon - forma padrão)");
        }

        /// <summary>Growlmon e Devidramon são ambos Champion (a própria aresta testada é
        /// Champion->Ultimate) - spawná-los direto no Lv 1 comete o MESMO erro de modelo que o
        /// Caso D já corrigiu pro Gaogamon (rota mais fraca != rota mais barata). Por isso cada
        /// rota aqui parte do Rookie de verdade (Guilmon/Elizamon) e sobe por TryToEvolve, igual
        /// ao resto do arquivo - primeira tentativa (spawn direto) deu WarGrowlmon->MetalGreymon
        /// pela rota Growlmon e nenhuma evolução pela rota Devidramon (PD/Vel de carry-over baixos
        /// demais sem a evolução real por trás).</summary>
        private void CaseI3_WarGrowlmon()
        {
            GD.Print("\n--- Caso I3: WarGrowlmon -> Medieval Dukemon / Gallantmon, via Growlmon (Guilmon) e via Devidramon (Elizamon) ---");

            const int guilmonId = 7;
            const int elizamonId = 84;

            DigimonInstance ViaGuilmon() // chega em Growlmon pela cadeia real - mesma receita do Caso A (só PA)
            {
                var d = SpawnNatural(guilmonId);
                GrowLeg(d, requiredLevel: 12, requiredAge: 2, stat: TrainingType.Attack, floor: 104);
                return d;
            }

            DigimonInstance ViaElizamon() // chega em Devidramon pela cadeia real - só Vel (assinatura)
            {
                var d = SpawnNatural(elizamonId);
                GrowLeg(d, requiredLevel: 12, requiredAge: 2, stat: TrainingType.Speed, floor: 104);
                return d;
            }

            ResetScratch();
            var duke1 = ViaGuilmon();
            Check(duke1.BaseData.Name == "Growlmon", $"I3-Guilmon: chegou em Growlmon vindo de Guilmon (só PA) - {duke1.BaseData.Name}");

            // Fase 5: Growlmon->MetalGreymon agora exige EA>=60 (CSV v5) - EA tem GROWTH=0 (só
            // sobe com treino de verdade), então sem treinar EA o MetalGreymon fica permanentemente
            // fora de alcance por essa rota, não importa quanto PA/HP/PD a batalha empurre por
            // cima - WarGrowlmon (sem exigência de EA) é quem sobra. Não precisa mais de
            // granularidade fina de batalha (GrowLegPrecise) pra isso - ver Caso I7 pro teste
            // dedicado dos dois lados (sem EA -> WarGrowlmon / com EA -> MetalGreymon).
            GrowLeg(duke1, requiredLevel: 20, requiredAge: 5,
                new (TrainingType, int)[] { (TrainingType.HealthPoints, 258), (TrainingType.Attack, 163), (TrainingType.Speed, 51) });

            Check(duke1.BaseData.Name == "WarGrowlmon", $"I3-Guilmon-WarGrowlmon: chegou em WarGrowlmon vindo de Growlmon (HP+PA+Vel, sem EA) - {duke1.BaseData.Name}");

            GrowLeg(duke1, requiredLevel: 28, requiredAge: 8, stat: TrainingType.Defense, floor: 180);

            Check(duke1.BaseData.Name == "Medieval Dukemon",
                $"I3a (Guilmon -> Growlmon -> WarGrowlmon -> só PD até 180, Lv 28): resultado {duke1.BaseData.Name} (esperado Medieval Dukemon)");

            ResetScratch();
            var duke2 = ViaElizamon();
            Check(duke2.BaseData.Name == "Devidramon", $"I3-Elizamon: chegou em Devidramon vindo de Elizamon (só Vel) - {duke2.BaseData.Name}");

            GrowLeg(duke2, requiredLevel: 20, requiredAge: 5, stat: TrainingType.Attack, floor: 201);

            Check(duke2.BaseData.Name == "WarGrowlmon", $"I3-Elizamon-WarGrowlmon: chegou em WarGrowlmon vindo de Devidramon (só PA) - {duke2.BaseData.Name}");

            GrowLeg(duke2, requiredLevel: 28, requiredAge: 8, stat: TrainingType.Defense, floor: 180);

            Check(duke2.BaseData.Name == "Medieval Dukemon",
                $"I3b (Elizamon -> Devidramon -> WarGrowlmon -> só PD até 180, Lv 28): resultado {duke2.BaseData.Name} (esperado Medieval Dukemon)");

            ResetScratch();
            var gallant = ViaGuilmon();
            GrowLeg(gallant, requiredLevel: 20, requiredAge: 5,
                new (TrainingType, int)[] { (TrainingType.HealthPoints, 258), (TrainingType.Attack, 163), (TrainingType.Speed, 51) });
            GrowLeg(gallant, requiredLevel: 28, requiredAge: 50);

            Check(gallant.BaseData.Name == "Gallantmon",
                $"I3c (Guilmon -> Growlmon -> WarGrowlmon -> sem mais treino, até a Idade 50): resultado {gallant.BaseData.Name} (esperado Gallantmon - forma padrão)");
        }

        /// <summary>Stingmon e Kabuterimon também são Champion - mesmo cuidado do Caso I3: sobem
        /// da cadeia real (Wormmon/Tentomon), não spawnados direto.</summary>
        private void CaseI4_JewelBeemon()
        {
            GD.Print("\n--- Caso I4: JewelBeemon -> Imperialdramon / GrandisKuwagamon, via Stingmon (Wormmon) e via Kabuterimon (Tentomon) ---");

            const int wormmonId = 197;
            const int tentomonId = 3;

            ResetScratch();
            var imp = SpawnNatural(wormmonId);
            GrowLeg(imp, requiredLevel: 12, requiredAge: 2, stat: TrainingType.Speed, floor: 98);

            Check(imp.BaseData.Name == "Stingmon", $"I4-Wormmon: chegou em Stingmon vindo de Wormmon (só Vel) - {imp.BaseData.Name}");

            GrowLeg(imp, requiredLevel: 20, requiredAge: 5, stat: TrainingType.Attack, floor: 172);

            Check(imp.BaseData.Name == "JewelBeemon", $"I4-Wormmon-JewelBeemon: chegou em JewelBeemon vindo de Stingmon (só PA) - {imp.BaseData.Name}");

            GrowLeg(imp, requiredLevel: 28, requiredAge: 8, stat: TrainingType.SpecialDefense, floor: 99);

            Check(imp.BaseData.Name == "Imperialdramon",
                $"I4a (Wormmon -> Stingmon -> JewelBeemon -> só ED até 99, Lv 28): resultado {imp.BaseData.Name} (esperado Imperialdramon, não GrandisKuwagamon)");

            ResetScratch();
            var grandis = SpawnNatural(tentomonId);
            GrowLeg(grandis, requiredLevel: 12, requiredAge: 13); // Tentomon -> Kabuterimon é o padrão

            Check(grandis.BaseData.Name == "Kabuterimon", $"I4-Tentomon: chegou em Kabuterimon vindo de Tentomon (padrão) - {grandis.BaseData.Name}");

            GrowLeg(grandis, requiredLevel: 20, requiredAge: 24); // Kabuterimon -> JewelBeemon é o padrão

            Check(grandis.BaseData.Name == "JewelBeemon", $"I4-Tentomon-JewelBeemon: chegou em JewelBeemon vindo de Kabuterimon (padrão) - {grandis.BaseData.Name}");
            GD.Print($"  [diagnóstico I4b pré] Lv{grandis.Level} Age{grandis.AgeInDays} HP{grandis.CurrentStats.HealthPoints} PA{grandis.CurrentStats.PhysicalDamage} EA{grandis.CurrentStats.SpecialDamage} ED{grandis.CurrentStats.SpecialDefense} Vel{grandis.CurrentStats.Speed}");

            GrowLeg(grandis, requiredLevel: 28, requiredAge: 8, stat: TrainingType.Attack, floor: 224);

            GD.Print($"  [diagnóstico I4b pós] Lv{grandis.Level} Age{grandis.AgeInDays} HP{grandis.CurrentStats.HealthPoints} PA{grandis.CurrentStats.PhysicalDamage} EA{grandis.CurrentStats.SpecialDamage} ED{grandis.CurrentStats.SpecialDefense} Vel{grandis.CurrentStats.Speed} nome={grandis.BaseData.Name}");
            Check(grandis.BaseData.Name == "GrandisKuwagamon",
                $"I4b (Tentomon -> Kabuterimon -> JewelBeemon -> só PA até 224, Lv 28): resultado {grandis.BaseData.Name} (esperado GrandisKuwagamon)");
        }

        private void CaseI5_Angewomon()
        {
            GD.Print("\n--- Caso I5: Angewomon -> Lilithmon, via Kudamon -> Angemon ---");

            const int kudamonId = 129;

            ResetScratch();

            var d = SpawnNatural(kudamonId);

            // Kudamon,Angemon: HP150 PA0 PD0 EA98 ED0 Vel80 - a assinatura é EA, mas HP150 e Vel80
            // também são exigidos (colunas do CSV são HP,PA,PD,EA,ED,Vel - ED aqui é 0, não Vel).
            // EA/ED têm GROWTH=0 por nível (só sobem com treino de verdade) - e o HP natural de um
            // Kudamon nunca-evoluído não cobre 150 sozinho até o Lv 12 (GROWTH HP=5/nível), então
            // HP entra no treino também.
            GrowLeg(d, requiredLevel: 12, requiredAge: 2,
                new (TrainingType, int)[] { (TrainingType.HealthPoints, 150), (TrainingType.SpecialAttack, 98), (TrainingType.Speed, 80) });

            Check(d.BaseData.Name == "Angemon", $"I5-0: chegou em Angemon vindo de Kudamon (HP+EA+Vel) - {d.BaseData.Name}");

            GrowLeg(d, requiredLevel: 20, requiredAge: 24); // Angemon -> Angewomon é o padrão (Caso C)

            Check(d.BaseData.Name == "Angewomon", $"I5-1: chegou em Angewomon (padrão de Angemon) - {d.BaseData.Name}");

            GrowLeg(d, requiredLevel: 28, requiredAge: 8, stat: TrainingType.SpecialDefense, floor: 108);

            Check(d.BaseData.Name == "Lilithmon",
                $"I5a (Kudamon -> Angemon -> Angewomon padrão -> só ED até 108, Lv 28): resultado {d.BaseData.Name} (esperado Lilithmon, não Ophanimon)");
        }

        private void CaseI6_Knightmon()
        {
            GD.Print("\n--- Caso I6: Knightmon -> Craniummon / Crusadermon, via Tentomon -> Kabuterimon ---");

            const int tentomonId = 3;

            DigimonInstance ChegarEmKnightmon()
            {
                var d = SpawnNatural(tentomonId);
                GrowLeg(d, requiredLevel: 12, requiredAge: 13); // Tentomon -> Kabuterimon é o padrão

                Check(d.BaseData.Name == "Kabuterimon", $"I6-0: chegou em Kabuterimon vindo de Tentomon (padrão) - {d.BaseData.Name}");

                GrowLeg(d, requiredLevel: 28, requiredAge: 8, stat: TrainingType.Defense, floor: 144);

                Check(d.BaseData.Name == "Knightmon", $"I6-1: chegou em Knightmon vindo de Kabuterimon (só PD) - {d.BaseData.Name}");

                return d;
            }

            ResetScratch();
            var crani = ChegarEmKnightmon();
            GrowLeg(crani, requiredLevel: 28, requiredAge: 50);

            Check(crani.BaseData.Name == "Craniummon",
                $"I6a (Tentomon -> Kabuterimon -> Knightmon -> sem treino, até a Idade 50): resultado {crani.BaseData.Name} (esperado Craniummon - forma padrão)");

            ResetScratch();
            var crusader = ChegarEmKnightmon();
            GrowLeg(crusader, requiredLevel: 36, requiredAge: 12, stat: TrainingType.Speed, floor: 147, maxBattles: 260);

            Check(crusader.BaseData.Name == "Crusadermon",
                $"I6b (Tentomon -> Kabuterimon -> Knightmon -> só Vel até 147, Lv 36 antes da Idade 50): resultado {crusader.BaseData.Name} (esperado Crusadermon)");
        }

        /// <summary>Fase 5: conserto do par Growlmon -> {MetalGreymon, WarGrowlmon} (CSV v5 - único
        /// stat que muda é o EA de MetalGreymon, 0 -> 60). Sem treinar EA (que tem GROWTH=0, nunca
        /// sobe sozinho), MetalGreymon fica permanentemente fora de alcance por essa rota e
        /// WarGrowlmon vira a evolução natural da linhagem; treinando só EA até 60, MetalGreymon.
        /// A rota já existente Agumon -> Greymon -> MetalGreymon (não tocada pelo CSV v5) continua
        /// funcionando do mesmo jeito de sempre.</summary>
        private void CaseI7_GrowlmonMetalGreymon()
        {
            GD.Print("\n--- Caso I7: Fase 5 - Growlmon -> WarGrowlmon (natural) / MetalGreymon (EA), + Greymon -> MetalGreymon ---");

            const int guilmonId = 7;
            const int agumonId = 1;

            DigimonInstance ViaGuilmon()
            {
                var d = SpawnNatural(guilmonId);
                GrowLeg(d, requiredLevel: 12, requiredAge: 2, stat: TrainingType.Attack, floor: 104);
                return d;
            }

            ResetScratch();
            var natural = ViaGuilmon();
            Check(natural.BaseData.Name == "Growlmon", $"I7-0: chegou em Growlmon vindo de Guilmon (só PA) - {natural.BaseData.Name}");

            GrowLeg(natural, requiredLevel: 20, requiredAge: 5,
                new (TrainingType, int)[] { (TrainingType.HealthPoints, 258), (TrainingType.Attack, 163), (TrainingType.Speed, 51) });

            Check(natural.BaseData.Name == "WarGrowlmon",
                $"I7a (Guilmon -> Growlmon -> sem treinar EA, até o Lv 20): resultado {natural.BaseData.Name} (esperado WarGrowlmon - forma natural da linhagem)");

            ResetScratch();
            var metal = ViaGuilmon();

            // "Só EA" é o treino ADICIONAL em cima da mesma base de HP/PA/PD que qualquer
            // Growlmon precisa pra chegar a esse ponto da árvore - sem isso nem WarGrowlmon nem
            // MetalGreymon têm HP/PA/PD suficiente (o nível sozinho não cobre, GROWTH por nível é
            // pequeno). Piso de MetalGreymon (Fase 6, com a cascata da Fase 5): HP258/PA177/PD77/EA60.
            GrowLeg(metal, requiredLevel: 20, requiredAge: 5,
                new (TrainingType, int)[] { (TrainingType.HealthPoints, 258), (TrainingType.Attack, 177), (TrainingType.Defense, 77), (TrainingType.SpecialAttack, 60) });

            Check(metal.BaseData.Name == "MetalGreymon",
                $"I7b (Guilmon -> Growlmon -> mesma base + EA até 60, Lv 20): resultado {metal.BaseData.Name} (esperado MetalGreymon)");

            // Rota já existente (Agumon -> Greymon -> MetalGreymon), não tocada pelo CSV v5 -
            // continua funcionando normalmente.
            ResetScratch();
            var viaGreymon = SpawnNatural(agumonId);
            GrowLeg(viaGreymon, requiredLevel: 12, requiredAge: 2,
                new (TrainingType, int)[] { (TrainingType.HealthPoints, 150), (TrainingType.Attack, 61), (TrainingType.Defense, 55), (TrainingType.Speed, 24) });

            Check(viaGreymon.BaseData.Name == "Greymon", $"I7-1: chegou em Greymon vindo de Agumon (HP+PA+PD+Vel) - {viaGreymon.BaseData.Name}");

            GrowLeg(viaGreymon, requiredLevel: 20, requiredAge: 5,
                new (TrainingType, int)[] { (TrainingType.HealthPoints, 248), (TrainingType.Attack, 191), (TrainingType.SpecialAttack, 30), (TrainingType.Speed, 36) });

            Check(viaGreymon.BaseData.Name == "MetalGreymon",
                $"I7c (Agumon -> Greymon -> MetalGreymon, rota já existente): resultado {viaGreymon.BaseData.Name} (esperado MetalGreymon - continua funcionando)");
        }

        /// <summary>Fase 6: Baby e InTraining por EA/ED. Antes desta fase, os requisitos de
        /// Baby->InTraining e InTraining->Rookie eram só HP/PA/PD/Vel - stats que sobem sozinhos
        /// com o nível - então a checagem (que só roda depois de batalha ou virada de dia, nunca
        /// durante o treino em si) sempre encontrava as DUAS irmãs já válidas, e a de Score maior
        /// vencia (checagem H nova do verificar.py). EA/ED têm GROWTH=0 (só sobem treinando de
        /// verdade), então agora cada irmã assina por EA, por ED, ou pelas duas, e o pai mantém
        /// uma forma padrão que sai sem treinar nada (presa pela Idade). Cada grupo abaixo é um
        /// pai com 2 ou mais saídas, testando cada uma pela cadeia real (Lv 1 direto - Baby/
        /// InTraining são o próprio estágio testado, sem precisar vir de mais atrás).</summary>
        private void CaseI8_Fase6BabyInTraining()
        {
            GD.Print("\n--- Caso I8: Fase 6 - Baby/InTraining por EA/ED (Tsunomon, Nyaromon, Koromon, Yuramon) ---");

            const int tsunomonId = 30;
            const int nyaromonId = 69;
            const int koromonId = 29;
            const int yuramonId = 27;

            // Tsunomon (InTraining) -> Gabumon (EA) / Kotemon (ED) / Elizamon (padrão, Idade 5)
            ResetScratch();
            var gabumon = SpawnNatural(tsunomonId);
            GrowLeg(gabumon, requiredLevel: 2, requiredAge: 0, stat: TrainingType.SpecialAttack, floor: 25);
            Check(gabumon.BaseData.Name == "Gabumon", $"I8a (Tsunomon -> só EA até 25): resultado {gabumon.BaseData.Name} (esperado Gabumon)");

            ResetScratch();
            var kotemon = SpawnNatural(tsunomonId);
            GrowLeg(kotemon, requiredLevel: 2, requiredAge: 0, stat: TrainingType.SpecialDefense, floor: 15);
            Check(kotemon.BaseData.Name == "Kotemon", $"I8b (Tsunomon -> só ED até 15): resultado {kotemon.BaseData.Name} (esperado Kotemon)");

            ResetScratch();
            var elizamon = SpawnNatural(tsunomonId);
            GrowLeg(elizamon, requiredLevel: 2, requiredAge: 5);
            Check(elizamon.BaseData.Name == "Elizamon", $"I8c (Tsunomon -> sem treino, até a Idade 5): resultado {elizamon.BaseData.Name} (esperado Elizamon - forma padrão)");

            // Nyaromon (Baby) -> Tokomon (EA) / Salamon (padrão, Idade 2)
            ResetScratch();
            var tokomon = SpawnNatural(nyaromonId);
            GrowLeg(tokomon, requiredLevel: 2, requiredAge: 0, stat: TrainingType.SpecialAttack, floor: 15);
            Check(tokomon.BaseData.Name == "Tokomon", $"I8d (Nyaromon -> só EA até 15): resultado {tokomon.BaseData.Name} (esperado Tokomon)");

            ResetScratch();
            var salamon = SpawnNatural(nyaromonId);
            GrowLeg(salamon, requiredLevel: 2, requiredAge: 2);
            Check(salamon.BaseData.Name == "Salamon", $"I8e (Nyaromon -> sem treino, até a Idade 2): resultado {salamon.BaseData.Name} (esperado Salamon - forma padrão)");

            // Koromon (InTraining) -> Agumon (EA) / Veemon (ED) / Agumon (Savers) (padrão, Idade 5)
            ResetScratch();
            var agumon = SpawnNatural(koromonId);
            GrowLeg(agumon, requiredLevel: 2, requiredAge: 0, stat: TrainingType.SpecialAttack, floor: 17);
            Check(agumon.BaseData.Name == "Agumon", $"I8f (Koromon -> só EA até 17): resultado {agumon.BaseData.Name} (esperado Agumon)");

            ResetScratch();
            var veemon = SpawnNatural(koromonId);
            GrowLeg(veemon, requiredLevel: 2, requiredAge: 0, stat: TrainingType.SpecialDefense, floor: 12);
            Check(veemon.BaseData.Name == "Veemon", $"I8g (Koromon -> só ED até 12): resultado {veemon.BaseData.Name} (esperado Veemon)");

            ResetScratch();
            var agumonSavers = SpawnNatural(koromonId);
            GrowLeg(agumonSavers, requiredLevel: 2, requiredAge: 5);
            Check(agumonSavers.BaseData.Name == "Agumon (Savers)", $"I8h (Koromon -> sem treino, até a Idade 5): resultado {agumonSavers.BaseData.Name} (esperado Agumon (Savers) - forma padrão)");

            // Yuramon (Baby) -> Minomon (EA) / Yokomon (ED) / Budmon (EA+ED) / Tanemon (padrão, Idade 2)
            ResetScratch();
            var minomon = SpawnNatural(yuramonId);
            GrowLeg(minomon, requiredLevel: 2, requiredAge: 0, stat: TrainingType.SpecialAttack, floor: 19);
            Check(minomon.BaseData.Name == "Minomon", $"I8i (Yuramon -> só EA até 19): resultado {minomon.BaseData.Name} (esperado Minomon)");

            ResetScratch();
            var yokomon = SpawnNatural(yuramonId);
            GrowLeg(yokomon, requiredLevel: 2, requiredAge: 0, stat: TrainingType.SpecialDefense, floor: 8);
            Check(yokomon.BaseData.Name == "Yokomon", $"I8j (Yuramon -> só ED até 8): resultado {yokomon.BaseData.Name} (esperado Yokomon)");

            // Budmon exige EA15+ED8+8 batalhas - mas Yokomon (ED8 só, sem exigir batalha) já sai
            // válido assim que ED chega a 8, então treinar ED ANTES de bater as 8 batalhas deixa
            // Yokomon dar o bote no primeiro checkpoint (batalha 1), antes de Budmon completar a
            // contagem. A ordem certa é bater as 8 batalhas PRIMEIRO (com ED ainda intocado, sem
            // risco de Yokomon disparar cedo), treinar EA/ED depois, e só então rodar 1 checkpoint
            // final - com tudo pronto ao mesmo tempo, o Score maior (Budmon 67 > Yokomon 57) decide.
            ResetScratch();
            var budmon = SpawnNatural(yuramonId);
            for (int i = 0; i < 8 && budmon.BattlesFought < 8 && budmon.BaseData.Id == yuramonId; i++)
                BattleCheckpointWithLevelUp(budmon);
            TrainUntilStat(budmon, TrainingType.SpecialAttack, s => s.SpecialDamage, 15);
            TrainUntilStat(budmon, TrainingType.SpecialDefense, s => s.SpecialDefense, 8);
            if (budmon.BaseData.Id == yuramonId)
                BattleCheckpointWithLevelUp(budmon);
            Check(budmon.BaseData.Name == "Budmon", $"I8k (Yuramon -> 8 batalhas primeiro, depois EA até 15 + ED até 8): resultado {budmon.BaseData.Name} (esperado Budmon)");

            ResetScratch();
            var tanemon = SpawnNatural(yuramonId);
            GrowLeg(tanemon, requiredLevel: 2, requiredAge: 2);
            Check(tanemon.BaseData.Name == "Tanemon", $"I8l (Yuramon -> sem treino, até a Idade 2): resultado {tanemon.BaseData.Name} (esperado Tanemon - forma padrão)");
        }
    }
}
