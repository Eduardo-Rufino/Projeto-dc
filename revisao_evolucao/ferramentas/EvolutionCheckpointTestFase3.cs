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
    /// Fase 3 da revisão de requisitos de evolução: prova, com o código real, que os CSV
    /// (já importados em Data/Evolutions/) resolvem o problema da Fase 1/2. Diferença central
    /// pra EvolutionCheckpointTest (Fase 2): aqui os Digimons NASCEM e SOBEM DE NÍVEL pela
    /// curva real (GainExperience/LevelUp) e o "treino" usa o TrainingSystem de verdade
    /// (TrainingSystem.Execute + DigimonInstance.ApplyTrainingResult) - inclusive o sorteio
    /// 25/50/25% e a Felicidade/Disciplina que o treino de verdade muda - em vez de escrever
    /// os stats diretamente. Só Felicidade/Disciplina em dois casos (C e E) são setados
    /// diretamente: ali são a VARIÁVEL INDEPENDENTE do próprio caso (testar o efeito de um
    /// valor de Felicidade/Disciplina específico), não um substituto do treino.
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

            GD.Print("===== EvolutionCheckpointTestFase3: início =====");

            CaseA_Guilmon();
            CaseB_Gaomon();
            CaseC_Angewomon();
            CaseD_MachGaogamon();
            CaseE_GigimonNoTie();
            CaseF_NovasFormasPadrao();

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

        /// <summary>Cria um Digimon e sobe até o nível pedido só pela curva real (GainExperience
        /// repetido -> LevelUp real: +2 PA, +2 PD, +1 Vel, +5 HP por nível). Os stats ficam
        /// exatamente onde a espécie + os níveis deixam - nenhum treino aconteceu ainda.
        /// AgeInDays/Happiness/Discipline são setados diretamente (não são "stat de combate"
        /// nenhuma evolução usa Happiness/Discipline como algo que "se treina pra", são
        /// variáveis de cuidado do jogador, não têm uma "curva real de treino" pra seguir).
        /// </summary>
        private static DigimonInstance SpawnNatural(int speciesId, int level, int age, int happiness = 50, int discipline = 50)
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

        /// <summary>Um treino de verdade: TrainingSystem.Execute + DigimonInstance.
        /// ApplyTrainingResult, exatamente o par que DigimonWorld.ProcessTraining chama (ver
        /// Fase 1, item 1) - RNG real (25/50/25%), +10 Felicidade e +5 Disciplina reais, 5 XP
        /// real (pode até disparar um LevelUp real se acumular o suficiente - deixado
        /// acontecer, não é suprimido). Reabastece a stamina direto (100) quando falta -
        /// atalho deliberado: regenerar 1 em 1 ponto (72/dia no jogo real) não muda NENHUM
        /// stat relevante pra decisão de evolução, só quanto tempo de jogo o treino "gastaria"
        /// - não é o mesmo tipo de atalho que setar PA/Vel/etc direto.</summary>
        private void TrainOnce(DigimonInstance d, TrainingType type)
        {
            if (d.Stamina < 10)
                d.Stamina = 100;

            var result = Gm.TrainingSystem.Execute(d, type);

            if (result.Success)
                d.ApplyTrainingResult(result);
        }

        /// <summary>Checkpoint real (SEM XP): fim de uma batalha 3x3 vencida fora de torneio,
        /// com time inimigo vazio de propósito - ver EvolutionCheckpointTest.BattleCheckpoint
        /// (Fase 2) pro porquê. RecordBattleResult ainda roda (incrementa BattlesFought de
        /// verdade), só o XP/LevelUp é que fica de fora.</summary>
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
        /// usado só quando o caso pede pra subir de nível por batalha.</summary>
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

        /// <summary>Checkpoint real: virada de dia (ClockSystem.DayPassed -> OnDayPassed).
        /// Precisa que o Digimon esteja no Center (EvaluateDailyDeathRisks itera Save.Center.
        /// Digimons).</summary>
        private void DayPassedCheckpoint(DigimonInstance member)
        {
            if (!Gm.Save.Center.Digimons.Contains(member))
                Gm.CenterService.AddDigimon(member);

            Gm.ClockSystem.Update(1440.0);

            // Cura qualquer doença na hora (Heal() real, mesma função que UseMedicine chama) -
            // sem isso, TryBecomeSick (aleatório, ~sorteado todo dia) podia deixar o Digimon
            // doente por vários dias seguidos e arriscar uma morte por negligência
            // (GameManager.EvaluateDailyDeathRisks, 20% ao dia após 5 dias doente seguidos) -
            // um risco real do jogo, mas não o que ESTE teste quer medir (só os requisitos de
            // evolução). Descoberto porque F/B3 falhavam de forma não-determinística até essa
            // correção - ver relatório da Fase 3.
            if (member.HealthState == HealthState.Sick)
                member.Heal();
        }

        /// <summary>Treina um único stat repetidamente (checkpoint real depois de cada treino)
        /// até evoluir ou até o teto de tentativas. Devolve quantos treinos foram aplicados.
        /// </summary>
        private int TrainUntilEvolved(DigimonInstance d, TrainingType type, int maxAttempts = 200)
        {
            int startId = d.BaseData.Id;

            for (int i = 0; i < maxAttempts && d.BaseData.Id == startId; i++)
            {
                TrainOnce(d, type);
                BattleCheckpoint(d);
            }

            return d.BaseData.Id != startId ? 1 : 0;
        }

        // ------------------------------------------------------------------ caso a) Guilmon

        private void CaseA_Guilmon()
        {
            GD.Print("\n--- Caso A: Guilmon (id 7) ---");

            const int guilmonId = 7;

            // a1: só PA (treino real) -> Growlmon. Idade fica em 2 o tempo todo (nunca chega
            // nos 13 que Dimetromon/padrão exige), então Dimetromon nunca disputa aqui.
            ResetScratch();

            var pa = SpawnNatural(guilmonId, level: 12, age: 2);

            Check(TrainUntilEvolved(pa, TrainingType.Attack) == 1 && pa.BaseData.Name == "Growlmon",
                $"A1 (só PA, treino real): resultado {pa.BaseData.Name} (esperado Growlmon) - " +
                $"PA final {pa.CurrentStats.PhysicalDamage}, HP final {pa.CurrentStats.HealthPoints}");

            // a2: nada de treino, só batalhas (sem XP) e viradas de dia até a Idade 13 -
            // Growlmon exige PA que o Digimon nunca treinou, então só Dimetromon (padrão)
            // pode disparar, exatamente na Idade 13.
            ResetScratch();

            var none = SpawnNatural(guilmonId, level: 12, age: 2);

            for (int day = 0; day < 11 && none.BaseData.Id == guilmonId; day++)
                DayPassedCheckpoint(none);

            Check(none.BaseData.Name == "Dimetromon",
                $"A2 (sem treino, até a Idade 13 por virada de dia): resultado {none.BaseData.Name} " +
                $"(esperado Dimetromon - forma padrão) - Idade final {none.AgeInDays}");
        }

        // ------------------------------------------------------------------ caso b) Gaomon

        private void CaseB_Gaomon()
        {
            GD.Print("\n--- Caso B: Gaomon (id 5) ---");

            const int gaomonId = 5;

            // b1: só Velocidade, subindo de nível por batalha até o Lv 18 -> Gladimon, sem
            // Gaogamon (exige PA nunca treinado) nem Garurumon (exige Idade 13, nunca avançada)
            // disparar antes. Treina o Vel ATÉ O ALVO PRIMEIRO (ainda no Lv 12, onde nenhuma
            // irmã pode disparar - Gaogamon exige PA que não treinamos, Garurumon exige Idade
            // que não avançamos) e só DEPOIS sobe de nível - assim a subida de nível (XP real,
            // não controlável ponto a ponto) não "atropela" o treino de Vel pela metade.
            ResetScratch();

            // Age 5 (não 2): Gladimon exige Idade >= 5 no CSV (as outras duas irmãs pedem só
            // Idade >= 2), então precisa começar aí ou nunca fica válida mesmo com Vel/PA prontos.
            var vel = SpawnNatural(gaomonId, level: 12, age: 5);
            string prematureName = null;

            TrainUntilStat(vel, TrainingType.Speed, s => s.Speed, 82);

            if (vel.BaseData.Id != gaomonId)
                prematureName = vel.BaseData.Name;

            for (int i = 0; i < 60 && vel.Level < 18 && vel.BaseData.Id == gaomonId; i++)
            {
                BattleCheckpointWithLevelUp(vel);

                if (vel.BaseData.Id != gaomonId && vel.Level < 18)
                    prematureName = vel.BaseData.Name;
            }

            Check(prematureName == null,
                $"B1a (nenhuma irmã deveria disparar antes do Lv 18): {(prematureName == null ? "nenhuma disparou antes" : $"{prematureName} disparou antes do esperado")}");

            Check(vel.BaseData.Name == "Gladimon",
                $"B1b (Vel treinado até {vel.CurrentStats.Speed}, chegou ao Lv {vel.Level}): resultado {vel.BaseData.Name} (esperado Gladimon) - " +
                $"PA final {vel.CurrentStats.PhysicalDamage} (nunca treinado, só cresceu por nível)");

            // b2: só PA (treino real), sem subir de nível -> Gaogamon.
            ResetScratch();

            var pa = SpawnNatural(gaomonId, level: 12, age: 2);

            Check(TrainUntilEvolved(pa, TrainingType.Attack) == 1 && pa.BaseData.Name == "Gaogamon",
                $"B2 (só PA, treino real): resultado {pa.BaseData.Name} (esperado Gaogamon)");

            // b3: nada -> Garurumon aos 13 dias.
            ResetScratch();

            var none = SpawnNatural(gaomonId, level: 12, age: 2);

            GD.Print($"  [debug] B3 antes: Lv{none.Level} Age{none.AgeInDays} PA{none.CurrentStats.PhysicalDamage} Vel{none.CurrentStats.Speed}");

            for (int day = 0; day < 11 && none.BaseData.Id == gaomonId; day++)
            {
                DayPassedCheckpoint(none);
                GD.Print($"  [debug] B3 dia {day}: Lv{none.Level} Age{none.AgeInDays} PA{none.CurrentStats.PhysicalDamage} Vel{none.CurrentStats.Speed} nome={none.BaseData.Name}");
            }

            Check(none.BaseData.Name == "Garurumon",
                $"B3 (sem treino, até a Idade 13): resultado {none.BaseData.Name} (esperado Garurumon - forma padrão) - " +
                $"PA final {none.CurrentStats.PhysicalDamage} (natural, sem treino)");
        }

        // ------------------------------------------------------------------ caso c) Angewomon

        private void CaseC_Angewomon()
        {
            GD.Print("\n--- Caso C: Angewomon (id 72) ---");

            const int angewomonId = 72;

            // Diagnóstico: qual Vel a subida de nível pura (sem treino nenhum, Lv1->28) dá pra
            // uma Angewomon? Magnadramon/Ophanimon/Lilithmon pedem Vel>=179 os TRÊS - se a
            // subida pura não chega lá, NENHUM dos três é alcançável só de nível (mesmo a forma
            // "padrão"), então testar "só EA"/"só ED" LITERALMENTE (sem tocar em Vel) não devia
            // conseguir nenhum dos dois mesmo. Reporta os dois cenários: literal (só o stat
            // descrito no caso) e ajustado (Vel também levado ao piso via treino real, isolando
            // a pergunta "a lógica de escolha entre as 3 funciona quando os stats permitem?").
            var probe = SpawnNatural(angewomonId, level: 28, age: 8);
            GD.Print($"  [diagnóstico] Angewomon Lv28 sem NENHUM treino: HP{probe.CurrentStats.HealthPoints} EA{probe.CurrentStats.SpecialDamage} Vel{probe.CurrentStats.Speed} " +
                "(Magnadramon/Ophanimon/Lilithmon pedem Vel>=179 - se o Vel acima for menor, é teto de nível, não bug do teste)");

            // c_EA (LITERAL): só Ataque Especial (treino real) -> Ophanimon. Felicidade 30 e 50.
            foreach (int happiness in new[] { 30, 50 })
            {
                ResetScratch();

                var ea = SpawnNatural(angewomonId, level: 28, age: 8, happiness: happiness);

                TrainUntilEvolved(ea, TrainingType.SpecialAttack);

                Check(ea.BaseData.Name == "Ophanimon",
                    $"C1-literal (só EA, Felicidade={happiness}): resultado {ea.BaseData.Name} (esperado Ophanimon; " +
                    $"Vel ficou em {ea.CurrentStats.Speed}/179 - se for isso que bloqueou, ver C1-ajustado abaixo)");
            }

            // c_EA (AJUSTADO): mesma coisa, mas o Vel também é treinado (de verdade) até 179
            // ANTES do EA - isola só a pergunta "a escolha entre as 3 formas funciona?".
            foreach (int happiness in new[] { 30, 50 })
            {
                ResetScratch();

                var ea = SpawnNatural(angewomonId, level: 28, age: 8, happiness: happiness);

                TrainUntilStat(ea, TrainingType.Speed, s => s.Speed, 179);
                TrainUntilEvolved(ea, TrainingType.SpecialAttack);

                Check(ea.BaseData.Name == "Ophanimon",
                    $"C1-ajustado (EA+Vel treinados de verdade, Felicidade={happiness}): resultado {ea.BaseData.Name} (esperado Ophanimon)");
            }

            // c_ED (LITERAL e AJUSTADO): só Defesa Especial (treino real) -> Lilithmon.
            ResetScratch();

            var edLiteral = SpawnNatural(angewomonId, level: 28, age: 8);

            TrainUntilEvolved(edLiteral, TrainingType.SpecialDefense);

            Check(edLiteral.BaseData.Name == "Lilithmon",
                $"C2-literal (só ED, treino real): resultado {edLiteral.BaseData.Name} (esperado Lilithmon; Vel {edLiteral.CurrentStats.Speed}/179)");

            ResetScratch();

            var edAjustado = SpawnNatural(angewomonId, level: 28, age: 8);

            TrainUntilStat(edAjustado, TrainingType.Speed, s => s.Speed, 179);
            TrainUntilEvolved(edAjustado, TrainingType.SpecialDefense);

            Check(edAjustado.BaseData.Name == "Lilithmon",
                $"C2-ajustado (ED+Vel treinados de verdade): resultado {edAjustado.BaseData.Name} (esperado Lilithmon)");

            // c_nada (LITERAL e AJUSTADO): nada -> Magnadramon aos 50 dias.
            ResetScratch();

            var noneLiteral = SpawnNatural(angewomonId, level: 28, age: 8);

            for (int day = 0; day < 43 && noneLiteral.BaseData.Id == angewomonId; day++)
                DayPassedCheckpoint(noneLiteral);

            Check(noneLiteral.BaseData.Name == "Magnadramon",
                $"C3-literal (sem treino, até a Idade 50): resultado {noneLiteral.BaseData.Name} (esperado Magnadramon; " +
                $"Vel {noneLiteral.CurrentStats.Speed}/179, Idade {noneLiteral.AgeInDays})");

            ResetScratch();

            var noneAjustado = SpawnNatural(angewomonId, level: 28, age: 8);

            TrainUntilStat(noneAjustado, TrainingType.Speed, s => s.Speed, 179);

            for (int day = 0; day < 43 && noneAjustado.BaseData.Id == angewomonId; day++)
                DayPassedCheckpoint(noneAjustado);

            Check(noneAjustado.BaseData.Name == "Magnadramon",
                $"C3-ajustado (Vel treinado de verdade, até a Idade 50): resultado {noneAjustado.BaseData.Name} (esperado Magnadramon - forma padrão)");
        }

        // ------------------------------------------------------------------ caso d) MachGaogamon

        private void CaseD_MachGaogamon()
        {
            GD.Print("\n--- Caso D: MachGaogamon (id 48) ---");

            const int machGaogamonId = 48;

            var probe = SpawnNatural(machGaogamonId, level: 28, age: 8);
            GD.Print($"  [diagnóstico] MachGaogamon Lv28 sem NENHUM treino: PA{probe.CurrentStats.PhysicalDamage} Vel{probe.CurrentStats.Speed} " +
                "(Gankoomon/MirageGaogamon pedem Vel>=136 - se o Vel acima for menor, é teto de nível, não bug do teste)");

            // d_PA (LITERAL): só Ataque (treino real) -> MirageGaogamon. Felic>=40 no CSV.
            foreach (int happiness in new[] { 30, 50 })
            {
                ResetScratch();

                var pa = SpawnNatural(machGaogamonId, level: 28, age: 8, happiness: happiness);

                TrainUntilEvolved(pa, TrainingType.Attack);

                Check(happiness >= 40 ? pa.BaseData.Name == "MirageGaogamon" : true,
                    $"D1-literal (só PA, Felicidade={happiness}): resultado {pa.BaseData.Name} " +
                    $"(esperado MirageGaogamon se Felic>=40; Vel ficou em {pa.CurrentStats.Speed}/136 - ver D1-ajustado)");
            }

            // d_PA (AJUSTADO, NATURAL): Vel também treinado de verdade até 136, e a Felicidade
            // é deixada subir sozinha com o treino (ApplyTrainingResult dá +10 por treino, de
            // verdade) - é o que acontece com um jogador de verdade treinando bastante: mesmo
            // começando com Felicidade baixa, ela sobe rápido e passa de 40 bem antes do PA
            // chegar em 226. Por isso as DUAS Felicidades iniciais convergem pro mesmo
            // resultado aqui - não é um bug do teste, é o próprio treino elevando a Felicidade.
            foreach (int happiness in new[] { 30, 50 })
            {
                ResetScratch();

                var pa = SpawnNatural(machGaogamonId, level: 28, age: 8, happiness: happiness);

                TrainUntilStat(pa, TrainingType.Speed, s => s.Speed, 136);
                TrainUntilEvolved(pa, TrainingType.Attack);

                Check(pa.BaseData.Name == "MirageGaogamon",
                    $"D1-ajustado-natural (PA+Vel treinados, Felicidade INICIAL={happiness}, mas sobe com o treino): resultado {pa.BaseData.Name} " +
                    $"(esperado MirageGaogamon nos dois - Felicidade final {pa.Happiness}, treino real já passa de 40 sozinho)");
            }

            // d_PA (AJUSTADO, ISOLADO): mesma coisa, mas a Felicidade é forçada bem na hora da
            // checagem (não deixada subir) - testa a regra do CSV em isolado, sem o efeito
            // colateral do treino: só aqui a Felicidade baixa realmente barra MirageGaogamon.
            ResetScratch();

            var isolatedLow = SpawnNatural(machGaogamonId, level: 28, age: 8);

            TrainUntilStat(isolatedLow, TrainingType.Speed, s => s.Speed, 136);
            TrainUntilStat(isolatedLow, TrainingType.Attack, s => s.PhysicalDamage, 226);
            isolatedLow.Happiness = 30;
            // Gankoomon é a forma PADRÃO daqui (não a "papel PA" - essa é MirageGaogamon) e
            // como todo padrão exige Idade >= teto-2 do estágio (Ultimate: 52-2=50), não os
            // mesmos 8 dias que as outras duas. Sem isso nenhuma das duas fica válida (Gankoomon
            // barrado pela Idade, MirageGaogamon pela Felicidade) - e foi exatamente isso que a
            // primeira versão deste teste descobriu (por engano, mas corretamente).
            isolatedLow.AgeInDays = 50;
            BattleCheckpoint(isolatedLow);

            Check(isolatedLow.BaseData.Name == "Gankoomon",
                $"D1-isolado (PA+Vel prontos, Felicidade forçada em 30 só na checagem): resultado {isolatedLow.BaseData.Name} " +
                "(esperado Gankoomon - MirageGaogamon exige Felic>=40; confirma que a regra existe, só não trava quem treina de verdade)");

            // d_nada (LITERAL e AJUSTADO): nada -> Gankoomon aos 50 dias.
            ResetScratch();

            var noneLiteral = SpawnNatural(machGaogamonId, level: 28, age: 8);

            for (int day = 0; day < 43 && noneLiteral.BaseData.Id == machGaogamonId; day++)
                DayPassedCheckpoint(noneLiteral);

            Check(noneLiteral.BaseData.Name == "Gankoomon",
                $"D2-literal (sem treino, até a Idade 50): resultado {noneLiteral.BaseData.Name} (esperado Gankoomon; Vel {noneLiteral.CurrentStats.Speed}/136)");

            ResetScratch();

            var noneAjustado = SpawnNatural(machGaogamonId, level: 28, age: 8);

            TrainUntilStat(noneAjustado, TrainingType.Speed, s => s.Speed, 136);

            for (int day = 0; day < 43 && noneAjustado.BaseData.Id == machGaogamonId; day++)
                DayPassedCheckpoint(noneAjustado);

            Check(noneAjustado.BaseData.Name == "Gankoomon",
                $"D2-ajustado (Vel treinado de verdade, até a Idade 50): resultado {noneAjustado.BaseData.Name} (esperado Gankoomon - forma padrão)");
        }

        // ------------------------------------------------------------------ caso e) Gigimon

        private void CaseE_GigimonNoTie()
        {
            GD.Print("\n--- Caso E: Gigimon (id 33) - sem mais empate de Score ---");

            const int gigimonId = 33;

            var results = new Dictionary<string, int>();

            for (int i = 0; i < 20; i++)
            {
                ResetScratch();

                // Guilmon (padrão, score 87) só precisa da Idade 5 e do que o nível já dá.
                // Elizamon (score 184) precisa de EA>=34, Vel>=26 e Disciplina<=45 - os dois
                // ficam válidos ao mesmo tempo de propósito, pra testar o desempate. A
                // Disciplina é forçada DEPOIS do treino (não antes): ApplyTrainingResult dá +5
                // de Disciplina por treino de verdade, então setá-la antes só pra ela subir de
                // novo com os treinos seguintes e nunca ficar de fato <=45 na hora da checagem.
                var d = SpawnNatural(gigimonId, level: 2, age: 5);

                // Elizamon também exige HP>=67 - o natural do Lv 2 (53) não chega lá, esqueci
                // isso na primeira versão do teste (só tinha treinado EA e Vel).
                TrainUntilStat(d, TrainingType.HealthPoints, s => s.HealthPoints, 67);
                TrainUntilStat(d, TrainingType.SpecialAttack, s => s.SpecialDamage, 34);
                TrainUntilStat(d, TrainingType.Speed, s => s.Speed, 26);

                d.Discipline = 30;
                BattleCheckpoint(d);

                string name = d.BaseData.Name;
                results[name] = results.GetValueOrDefault(name) + 1;
            }

            GD.Print("  Resultado das 20 checagens: " + string.Join(", ", results.Select(kv => $"{kv.Key}={kv.Value}")));

            Check(results.Count == 1 && results.ContainsKey("Elizamon"),
                $"E (20 checagens, Guilmon e Elizamon ambos válidos): resultado(s) {string.Join("/", results.Keys)} " +
                "(esperado: sempre Elizamon, score 184 > 87 - sem mais sorteio, já que os scores não empatam mais)");
        }

        /// <summary>Treina um stat especifico (via TrainingSystem/ApplyTrainingResult reais)
        /// até ele alcançar um piso, sem checar evolução no meio (usado quando vários stats
        /// precisam ser preparados ANTES do checkpoint, pra não evoluir cedo demais só com o
        /// primeiro stat pronto).</summary>
        private void TrainUntilStat(DigimonInstance d, TrainingType type, Func<BaseStats, int> read, int floor, int maxAttempts = 100)
        {
            for (int i = 0; i < maxAttempts && read(d.CurrentStats) < floor; i++)
                TrainOnce(d, type);
        }

        // ------------------------------------------------------------------ caso f) novas formas padrão

        private void CaseF_NovasFormasPadrao()
        {
            GD.Print("\n--- Caso F: Rookies que só tinham saída premium (agora com forma padrão) ---");

            CheckDefaultAtAge13(132, "Dracomon", "Coredramon (Green)");
            CheckDefaultAtAge13(139, "Veemon", "Veedramon");
            CheckDefaultAtAge13(149, "Dorumon", "Dorugamon");
            CheckDefaultAtAge13(163, "Hackmon", "BaoHackmon");
        }

        private void CheckDefaultAtAge13(int speciesId, string speciesName, string expectedForm)
        {
            ResetScratch();

            var d = SpawnNatural(speciesId, level: 12, age: 2);

            GD.Print($"  [debug] {speciesName} antes: Lv{d.Level} Age{d.AgeInDays} HP{d.CurrentStats.HealthPoints} PA{d.CurrentStats.PhysicalDamage} PD{d.CurrentStats.PhysicalDefense} Vel{d.CurrentStats.Speed} Disc{d.Discipline} Happ{d.Happiness}");

            for (int day = 0; day < 11 && d.BaseData.Id == speciesId; day++)
                DayPassedCheckpoint(d);

            GD.Print($"  [debug] {speciesName} depois: Lv{d.Level} Age{d.AgeInDays} HealthState={d.HealthState} nome final={d.BaseData.Name}");

            Check(d.BaseData.Name == expectedForm,
                $"F ({speciesName}, sem treino, até a Idade 13): resultado {d.BaseData.Name} (esperado {expectedForm} - forma padrão)");
        }
    }
}
