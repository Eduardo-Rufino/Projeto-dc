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
    /// Prova, no código real do jogo (Fase 2 da revisão de requisitos de evolução), que a
    /// escolha entre irmãs acontece exatamente como a Fase 1 confirmou: NÃO existe checagem
    /// depois do treino autônomo (DigimonWorld.ProcessTraining -> ApplyTrainingResult, sem
    /// TryToEvolve); só existem dois pontos de checagem reais - GameManager.
    /// ApplyTeamBattleReward (batalha fora de torneio) e GameManager.OnDayPassed (virada de
    /// dia, via ClockSystem.DayPassed). Este arquivo NÃO reimplementa a lógica de evolução -
    /// todo o "quem evolui pra quê" passa pelas funções reais (GameManager.TryToEvolve ->
    /// EvolutionSystem.TryToEvolve), só o "treino acumulado sem checar" é simulado
    /// diretamente nos stats (correto, porque é exatamente isso que o treino autônomo faz).
    ///
    /// Convenção herdada de Scripts/Systems/Audit/GameAudit.cs (não existe GDScript neste
    /// projeto - é 100% C#; ver relatório da Fase 2 sobre esse desvio da instrução original).
    /// Uso: godot --headless --path . res://revisao_evolucao/ferramentas/EvolutionCheckpointTest.tscn
    /// </summary>
    public partial class EvolutionCheckpointTest : Node
    {
        private static readonly BindingFlags NonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private static GameManager Gm => GameManager.Instance;
        private static DatabaseManager Db => DatabaseManager.Instance;

        private int _ok, _fail;

        public override void _Ready() => CallDeferred(nameof(Start));

        private async void Start()
        {
            await Frames(30);

            GD.Print("===== EvolutionCheckpointTest: início =====");

            CaseA_GuilmonVsDimetromon();
            CaseB_GaomonVsGladimon();
            CaseC_AngewomonVsOphanimon();
            CaseD_MachGaogamonVsMirageGaogamon();
            CaseE_GigimonTie();

            GD.Print($"===== EvolutionCheckpointTest: {_ok} ok, {_fail} falha(s) =====");
            GetTree().Quit(_fail > 0 ? 1 : 0);
        }

        private async Task Frames(int n)
        {
            for (int i = 0; i < n; i++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        // ------------------------------------------------------------------ infraestrutura (igual a GameAudit.ResetScratch)

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

        /// <summary>Monta um DigimonInstance com Experience/ExperienceToNextLevel corretos pro
        /// nível pedido (via GainExperience real, repetido a partir do Lv1 - é a MESMA curva
        /// que LevelUp usa) e SÓ DEPOIS sobrescreve os stats/idade/humor pros valores do caso -
        /// exatamente como um Digimon que nasceu, subiu de nível treinando um pouco de tudo, e
        /// cujo estado atual é o descrito no caso.</summary>
        private static DigimonInstance MakeAt(
            int speciesId, int level, int age,
            int hp, int pa, int pd, int ea, int ed, int vel,
            int happiness = 50, int discipline = 50, int battlesFought = 0, int battlesWon = 0)
        {
            var species = Db.GetDigimon(speciesId);
            var d = new DigimonInstance(species);

            while (d.Level < level)
                d.GainExperience(d.ExperienceToNextLevel);

            d.AgeInDays = age;
            d.CurrentStats.HealthPoints = hp;
            d.CurrentStats.PhysicalDamage = pa;
            d.CurrentStats.PhysicalDefense = pd;
            d.CurrentStats.SpecialDamage = ea;
            d.CurrentStats.SpecialDefense = ed;
            d.CurrentStats.Speed = vel;
            d.MaxHealthPoints = hp;
            d.CurrentHealthPoints = hp;
            d.Happiness = happiness;
            d.Discipline = discipline;
            d.BattlesFought = battlesFought;
            d.BattlesWon = battlesWon;

            return d;
        }

        /// <summary>Checkpoint real nº1 (SEM XP): fim de uma batalha 3x3 vencida FORA de
        /// torneio - GameManager.ApplyTeamBattleReward(BattleResult.PlayerWon), o mesmo método
        /// que BattleArena.OnTeamBattleFinished chama de verdade. PlayerBattleTeam/
        /// EnemyBattleTeam têm setter privado (por design - só a própria montagem de batalha
        /// deveria setá-los), então usa reflection só pra ARMAR o cenário, igual
        /// GameAudit.ResetScratch já faz com Save/ClockSystem/CenterService; a decisão de
        /// evolução em si roda 100% pelo código real (nenhuma chamada a EvolutionSystem por
        /// aqui). EnemyBattleTeam vazio é de propósito: BattleRewardCalculator.CalculateForTeam
        /// devolve Experience=0 pra time inimigo vazio (guarda no início do método), então
        /// "if (xpGained > 0) member.GainExperience(...)" nunca roda - o resto do checkpoint
        /// (Felicidade, TryToEvolve) é idêntico a uma batalha de verdade. Isolar o XP evita
        /// contaminar os stats cuidadosamente montados de cada caso com level-ups reais
        /// inesperados (foi exatamente isso que quebrou a primeira versão deste teste - ver
        /// relatório da Fase 2: com um inimigo de verdade, o XP de uma única vitória já era
        /// suficiente pra subir vários níveis num Digimon Rookie/InTraining de nível baixo,
        /// mudando PA/PD/Vel/HP por baixo dos panos antes da checagem rodar).</summary>
        private void BattleCheckpoint(DigimonInstance member)
        {
            var t = typeof(GameManager);

            t.GetProperty("PlayerBattleTeam").GetSetMethod(true)
                .Invoke(Gm, new object[] { new List<DigimonInstance> { member } });
            t.GetProperty("EnemyBattleTeam").GetSetMethod(true)
                .Invoke(Gm, new object[] { new List<DigimonInstance>() });

            Gm.ApplyTeamBattleReward(BattleResult.PlayerWon);
        }

        /// <summary>Checkpoint real nº1 (COM XP): mesma função, mas com um inimigo de verdade
        /// no time adversário - usado só no caso B, onde subir de nível de propósito (via
        /// GainExperience/LevelUp reais) é o ponto central do caso. Inimigo Mega bem acima do
        /// nível do jogador: XP generoso (multiplicador 1.5x do teto de 900) pra levar poucas
        /// batalhas pra cruzar o limiar de cada nível, sem precisar prever o valor exato da
        /// curva de XP.</summary>
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

        /// <summary>Checkpoint real nº2: virada de dia (ClockSystem.DayPassed ->
        /// GameManager.OnDayPassed -> AdvanceDay/TryBecomeSick/EvaluateDailyDeathRisks/
        /// TryToEvolve). Precisa que o Digimon já esteja no Center (EvaluateDailyDeathRisks
        /// itera Save.Center.Digimons), senão OnDayPassed nem o vê.</summary>
        private void DayPassedCheckpoint(DigimonInstance member)
        {
            if (!Gm.Save.Center.Digimons.Contains(member))
                Gm.CenterService.AddDigimon(member);

            Gm.ClockSystem.Update(1440.0);
        }

        // ------------------------------------------------------------------ caso a) Guilmon

        private void CaseA_GuilmonVsDimetromon()
        {
            GD.Print("\n--- Caso A: Guilmon mirando Growlmon (id 18) x Dimetromon (id 85) ---");

            const int guilmonId = 7;

            // Sub-caso A1 (LITERAL, como escrito no caso): HP fixo em 154, só PA sobe até 84+.
            // Growlmon exige HP>=165 e Dimetromon HP>=166 - o enunciado nunca satisfaz nenhum
            // dos dois. Contradição relatada na Fase 2 (ver relatório) - documentando aqui o
            // comportamento real: nenhuma evolução dispara.
            ResetScratch();

            var literal = MakeAt(guilmonId, level: 12, age: 2, hp: 154, pa: 84, pd: 44, ea: 9, ed: 15, vel: 42);

            BattleCheckpoint(literal);

            Check(literal.BaseData.Id == guilmonId,
                $"A1 (literal, HP=154 fixo): com PA=84 e HP=154 (abaixo do exigido por Growlmon/165 e Dimetromon/166), " +
                $"NADA evolui - permanece {literal.BaseData.Name} (esperado: continuar Guilmon, confirma a contradição do enunciado)");

            // Sub-caso A2 (ajustado): sobe HP pra 166 (satisfaz as duas irmãs) - representa o
            // treino de fundo em HP que o enunciado pressupõe mas não detalha - e varre PA de
            // 65 a 84, um ponto por vez, checando (via batalha) depois de CADA ponto.
            ResetScratch();

            var sweep = MakeAt(guilmonId, level: 12, age: 2, hp: 166, pa: 64, pd: 44, ea: 9, ed: 15, vel: 42);

            int firedAtPa = -1;

            for (int pa = 65; pa <= 90 && sweep.BaseData.Id == guilmonId; pa++)
            {
                sweep.CurrentStats.PhysicalDamage = pa;
                BattleCheckpoint(sweep);

                if (sweep.BaseData.Id != guilmonId)
                    firedAtPa = pa;
            }

            Check(firedAtPa == 81 && sweep.BaseData.Name == "Dimetromon",
                $"A2 (varredura de PA, HP=166 fixo): evoluiu em PA={firedAtPa} para {sweep.BaseData.Name} " +
                "(esperado: PA=81, Dimetromon - score 305 > Growlmon score 294)");

            // Sub-caso A3: mesma varredura, mas via checkpoint de VIRADA DE DIA em vez de
            // batalha - prova que os dois pontos de checagem reais levam à mesma decisão
            // (a lógica é a mesma função, só o gatilho muda).
            ResetScratch();

            var sweepDay = MakeAt(guilmonId, level: 12, age: 2, hp: 166, pa: 80, pd: 44, ea: 9, ed: 15, vel: 42);

            DayPassedCheckpoint(sweepDay);
            Check(sweepDay.BaseData.Id == guilmonId, "A3a (virada de dia, PA=80): ainda não evoluiu (Dimetromon exige PA>=81)");

            sweepDay.CurrentStats.PhysicalDamage = 81;
            DayPassedCheckpoint(sweepDay);
            Check(sweepDay.BaseData.Name == "Dimetromon", $"A3b (virada de dia, PA=81): evoluiu para {sweepDay.BaseData.Name} (esperado Dimetromon)");

            // Sub-caso A4: mesmo ponto de partida, mas treino "em lote" (PA salta direto de 64
            // pra 84 - equivalente a um K grande) e UMA só checagem. Se Dimetromon vencesse só
            // por "chegar primeiro" cronologicamente, aqui ele perderia (as duas ficam válidas
            // ao mesmo tempo). Não é isso que acontece: o Score de Dimetromon (305) já é maior
            // que o de Growlmon (294) mesmo com as duas simultaneamente válidas - a causa raiz
            // não é só timing, é a fórmula do Score favorecer quem pede mais stats.
            ResetScratch();

            var batch = MakeAt(guilmonId, level: 12, age: 2, hp: 166, pa: 84, pd: 44, ea: 9, ed: 15, vel: 42);

            BattleCheckpoint(batch);

            Check(batch.BaseData.Name == "Dimetromon",
                $"A4 (treino em lote, PA direto pra 84 - as duas ficam válidas ao mesmo tempo): resultado {batch.BaseData.Name} " +
                "(esperado Dimetromon mesmo assim - comprova que é o Score, não só a ordem cronológica)");
        }

        // ------------------------------------------------------------------ caso b) Gaomon

        private void CaseB_GaomonVsGladimon()
        {
            GD.Print("\n--- Caso B: Gaomon mirando Gladimon (Lv 18, id 146) x Gaogamon (id 23) x Garurumon (id 14) ---");

            const int gaomonId = 5;

            ResetScratch();

            var d = MakeAt(gaomonId, level: 12, age: 2, hp: 154, pa: 68, pd: 42, ea: 9, ed: 15, vel: 32);

            // Fase 1: treina só PA até 92, ainda no Lv 12 - Gaogamon (HP165,Vel37) e Garurumon
            // (HP166,Vel41) continuam inalcançáveis aqui (HP=154 e Vel=32 ficam abaixo dos dois
            // o tempo todo), então nada deveria evoluir só com isso.
            for (int pa = 69; pa <= 92; pa++)
            {
                d.CurrentStats.PhysicalDamage = pa;
                BattleCheckpoint(d);
            }

            Check(d.BaseData.Id == gaomonId,
                $"B1 (PA=92, ainda Lv 12, HP=154/Vel=32 sem treino): permanece {d.BaseData.Name} " +
                "(esperado: continuar Gaomon - HP e Vel ainda não alcançam nenhuma irmã)");

            // Fase 2: sobe de nível só por batalha (LevelUp real: +2 PA, +2 PD, +1 Vel, +5 HP
            // por nível) até o Lv 18 ou até evoluir, o que vier primeiro - cada batalha é um
            // checkpoint real.
            int firedAtLevel = -1;

            for (int i = 0; i < 40 && d.Level < 18 && d.BaseData.Id == gaomonId; i++)
            {
                BattleCheckpointWithLevelUp(d);

                if (d.BaseData.Id != gaomonId)
                    firedAtLevel = d.Level;
            }

            Check(firedAtLevel > 0 && firedAtLevel < 18 && d.BaseData.Name == "Gaogamon",
                $"B2 (subindo por batalhas): evoluiu no Lv {firedAtLevel} para {d.BaseData.Name} " +
                "(esperado: Gaogamon, antes do Lv 18 - Garurumon fica pra trás por Vel e Gladimon por precisar do Lv 18 exato)");
        }

        // ------------------------------------------------------------------ caso c) Angewomon

        private void CaseC_AngewomonVsOphanimon()
        {
            GD.Print("\n--- Caso C: Angewomon mirando Magnadramon (id 73) x Ophanimon (id 178) ---");

            const int angewomonId = 72;

            // Felicidade >= 40: as duas ficam válidas (Magnadramon não tem exigência de
            // felicidade) - Ophanimon vence por Score (1076 > 1036, por causa do MinHappiness).
            ResetScratch();

            var happy = MakeAt(angewomonId, level: 28, age: 8, hp: 512, pa: 0, pd: 0, ea: 264, ed: 0, vel: 224, happiness: 50);

            BattleCheckpoint(happy);

            Check(happy.BaseData.Name == "Ophanimon",
                $"C1 (Felicidade=50, >=40): resultado {happy.BaseData.Name} (esperado Ophanimon)");

            // Felicidade < 40: só Magnadramon continua válida (Ophanimon exige Felic>=40).
            ResetScratch();

            var sad = MakeAt(angewomonId, level: 28, age: 8, hp: 512, pa: 0, pd: 0, ea: 264, ed: 0, vel: 224, happiness: 30);

            BattleCheckpoint(sad);

            Check(sad.BaseData.Name == "Magnadramon",
                $"C2 (Felicidade=30, <40): resultado {sad.BaseData.Name} (esperado Magnadramon - Ophanimon fica inválida)");
        }

        // ------------------------------------------------------------------ caso d) MachGaogamon

        private void CaseD_MachGaogamonVsMirageGaogamon()
        {
            GD.Print("\n--- Caso D: MachGaogamon mirando Gankoomon (id 162) x MirageGaogamon (id 176) ---");

            const int machGaogamonId = 48;

            ResetScratch();

            var happy = MakeAt(machGaogamonId, level: 28, age: 8, hp: 712, pa: 259, pd: 0, ea: 0, ed: 0, vel: 136, happiness: 50);

            BattleCheckpoint(happy);

            Check(happy.BaseData.Name == "MirageGaogamon",
                $"D1 (Felicidade=50, >=40): resultado {happy.BaseData.Name} (esperado MirageGaogamon)");

            // Verificação extra (não pedida explicitamente no caso d, adicionada por simetria
            // com o caso c - ver relatório): com Felicidade < 40 o padrão deveria se inverter.
            ResetScratch();

            var sad = MakeAt(machGaogamonId, level: 28, age: 8, hp: 712, pa: 259, pd: 0, ea: 0, ed: 0, vel: 136, happiness: 30);

            BattleCheckpoint(sad);

            Check(sad.BaseData.Name == "Gankoomon",
                $"D2 (extra, Felicidade=30, <40): resultado {sad.BaseData.Name} (esperado Gankoomon)");
        }

        // ------------------------------------------------------------------ caso e) Gigimon

        private void CaseE_GigimonTie()
        {
            GD.Print("\n--- Caso E: Gigimon mirando Guilmon (id 7) x Elizamon (id 84) - empate de Score (125 x 125) ---");

            const int gigimonId = 33;

            var results = new Dictionary<string, int>();

            for (int i = 0; i < 20; i++)
            {
                ResetScratch();

                var d = MakeAt(gigimonId, level: 2, age: 0, hp: 67, pa: 30, pd: 0, ea: 30, ed: 0, vel: 26);

                BattleCheckpoint(d);

                string name = d.BaseData.Name;
                results[name] = results.GetValueOrDefault(name) + 1;
            }

            GD.Print("  Resultado das 20 checagens: " + string.Join(", ", results.Select(kv => $"{kv.Key}={kv.Value}")));

            Check(results.Count > 1 && results.ContainsKey("Guilmon") && results.ContainsKey("Elizamon"),
                $"E (20 checagens independentes, mesmo estado inicial): saiu mais de um resultado ({string.Join("/", results.Keys)}) " +
                "- confirma o empate de Score (125 x 125) e o sorteio aleatório em EvolutionSystem.SelectBestEvolution");
        }
    }
}
