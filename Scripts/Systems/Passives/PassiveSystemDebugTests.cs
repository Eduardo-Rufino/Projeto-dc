using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Systems.Battle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Passives
{
    /// <summary>
    /// Bateria de verificação manual do Sistema de Passivas (ver PASSIVAS_SPEC.md na raiz do
    /// projeto) - não é um framework de teste (o projeto não tem um, ver AGENTS.md), é uma
    /// checagem via log que um agente/dev roda sob demanda (variável de ambiente
    /// DEBUG_TEST_PASSIVES, mesmo padrão do DEBUG_OPEN_SCREEN em Center.cs) sem afetar o
    /// jogo normal. Cresce a cada etapa da implementação - cada Run() cobre só a etapa já
    /// implementada até o momento.
    /// </summary>
    public static class PassiveSystemDebugTests
    {
        private static int _failures;

        public static void Run()
        {
            _failures = 0;

            GD.Print("===== PassiveSystemDebugTests: início =====");

            Test_CatalogLoaded();
            Test_RollFiltersByRole();
            Test_RollFiltersBySupportType();
            Test_RollAnySupportType();
            Test_EmptyPoolReturnsNull();
            Test_IncompatiblePoolReturnsNull();
            Test_RealSpeciesRollsFromPopulatedPool();
            Test_NonexistentSpeciesDoesNotThrow();
            Test_EvolveRerollsForNewFormRole();

            // Etapa 2 - hooks baratos no pipeline de dano (DamageCalculator é estático e não
            // depende da árvore de cena, então dá pra testar direto, sem BattleArena).
            Test_CouracaReducesFinalDamage();
            Test_PrecisaoLetalCritRate();
            Test_FioDaLaminaCritMultiplier();
            Test_RompeGuardaDamageGainCapped();

            // Etapa 3 - estado por-combate (stacks/contadores/flags "uma vez por luta").
            Test_FuriaCrescenteStacksAndDecays();
            Test_GolpePesadoCounterAndBonus();
            Test_ExecucaoBonusAgainstLowHp();
            Test_GolpeSorrateiroOnlyFirstHit();
            Test_AlvoMarcadoAppliesSpeedDebuff();
            Test_UltimaTrincheiraTriggersOnceBelowThreshold();
            Test_TeimosiaScalesWithMissingHp();
            Test_EspinhosReflectsToMeleeOnly();

            // Etapa 4 - IA de movimento. Só a parte que vive em DamageCalculator/
            // BattleCombatant é testável sem árvore de cena (BattleUnit é Node2D) - o resto
            // (dash/kiting/targeting/cooldown de verdade) foi validado com batalhas reais.
            Test_InvestidaConsumesFlagOnce();
            Test_PosturaFirmeBonusDamage();
            Test_FixacaoConsecutiveHitsIncrement();

            // Etapa 5 - Taunt cruzado e stacking especial. Dupla Voz/Marca Dupla vivem
            // inteiras em BattleUnit (precisam de time/posição, Node-dependente) - validadas
            // com batalha real, não aqui.
            Test_GritoDeGuerraReducesDamageWhenTaunted();
            Test_RessonanciaStackingCurve();

            // Etapa 6 - Vigília (escudo). SelectTarget/ApplyVigiliaShield vivem em
            // BattleUnit (Node-dependente, validados com batalha real) - aqui só o consumo
            // do escudo no pipeline de dano, que é puro.
            Test_ShieldAbsorbsDamageFully();
            Test_ShieldPartialAbsorptionThenHp();
            Test_ShieldDoesNotAffectEspinhosReflection();

            // Etapa 7 - on_tick (Fôlego, Aura Vital). A curva de decaimento e o acumulador
            // de regen contínua vivem em BattleCombatant (puro) - totalmente testável.
            // Aura Vital reaproveita o MESMO ApplyContinuousRegen, só a parte espacial ("quem
            // está perto") vive em BattleUnit (Node-dependente, validada com batalha real).
            Test_HealEffectivenessMultiplierCurve();
            Test_FolegoRegeneratesOverTime();
            Test_FolegoDoesNotExceedMaxHp();

            // Sede de Batalha e Projétil Perfurante ficaram de fora das etapas 2/4 por
            // engano (Warrior e Ranged, respectivamente) - adicionados depois, mesmo padrão
            // de teste das outras passivas de dano/cura puras. Projétil Perfurante escolhe o
            // 2º alvo por posição (BattleUnit, Node-dependente) - só validado em batalha real.
            Test_SedeDeBatalhaHealsAttacker();
            Test_SedeDeBatalhaRespectsR2Decay();

            if (_failures == 0)
                GD.Print("===== PassiveSystemDebugTests: TODOS OS TESTES PASSARAM =====");
            else
                GD.PrintErr($"===== PassiveSystemDebugTests: {_failures} TESTE(S) FALHARAM =====");
        }

        private static void Check(bool condition, string testName, string detail = "")
        {
            if (condition)
            {
                GD.Print($"[OK] {testName}");
            }
            else
            {
                _failures++;
                GD.PrintErr($"[FALHOU] {testName} {detail}");
            }
        }

        private static void Test_CatalogLoaded()
        {
            int count = DatabaseManager.Instance.GetAllPassives().Count();

            Check(count == 31, "CatalogLoaded: 31 passivas carregadas", $"(veio {count})");
        }

        private static DigimonData BuildSpecies(
            RoleType role,
            SupportType supportType,
            List<PassiveType> pool)
        {
            return new DigimonData
            {
                Id = 99999,
                Code = "test_species",
                Name = "TestSpecies",
                Stage = DigimonStage.Rookie,
                Role = role,
                SupportType = supportType,
                Passives = pool,
                BaseStats = new BaseStats
                {
                    HealthPoints = 100,
                    PhysicalDamage = 20,
                    PhysicalDefense = 20,
                    SpecialDamage = 20,
                    SpecialDefense = 20,
                    Speed = 20
                }
            };
        }

        private static void Test_RollFiltersByRole()
        {
            // Couraca é Tank - pool declara ela por engano numa espécie Warrior. O sorteio
            // nunca deve devolvê-la.
            var species = BuildSpecies(
                RoleType.Warrior,
                default,
                new List<PassiveType> { PassiveType.Investida, PassiveType.GolpePesado, PassiveType.Couraca }
            );

            var results = new HashSet<PassiveType>();

            for (int i = 0; i < 200; i++)
            {
                var rolled = PassiveSystem.RollRandomPassive(species);

                if (rolled.HasValue)
                    results.Add(rolled.Value);
            }

            bool onlyWarriorPassives = results.All(r => r == PassiveType.Investida || r == PassiveType.GolpePesado);
            bool neverNull = results.Count > 0;

            Check(neverNull, "RollFiltersByRole: nunca devolveu null com pool compatível não-vazio");
            Check(onlyWarriorPassives, "RollFiltersByRole: nunca sorteou Couraca (Tank) numa espécie Warrior", $"(resultados: {string.Join(",", results)})");
        }

        private static void Test_RollFiltersBySupportType()
        {
            // Ressonancia é Buffer-only - pool declara ela por engano numa espécie Healer.
            var species = BuildSpecies(
                RoleType.Support,
                SupportType.Healer,
                new List<PassiveType> { PassiveType.Vigilia, PassiveType.MaosFirmes, PassiveType.Ressonancia }
            );

            var results = new HashSet<PassiveType>();

            for (int i = 0; i < 200; i++)
            {
                var rolled = PassiveSystem.RollRandomPassive(species);

                if (rolled.HasValue)
                    results.Add(rolled.Value);
            }

            bool onlyHealerPassives = results.All(r => r == PassiveType.Vigilia || r == PassiveType.MaosFirmes);

            Check(onlyHealerPassives, "RollFiltersBySupportType: nunca sorteou Ressonancia (Buffer) numa espécie Healer", $"(resultados: {string.Join(",", results)})");
        }

        private static void Test_RollAnySupportType()
        {
            // AuraVital não declara SupportTypes (vazio = qualquer subtipo) - deve poder
            // sortear pra um Buffer normalmente.
            var species = BuildSpecies(
                RoleType.Support,
                SupportType.Buffer,
                new List<PassiveType> { PassiveType.AuraVital }
            );

            var rolled = PassiveSystem.RollRandomPassive(species);

            Check(rolled == PassiveType.AuraVital, "RollAnySupportType: AuraVital (sem restrição de subtipo) sorteia normalmente pra um Buffer");
        }

        private static void Test_EmptyPoolReturnsNull()
        {
            var species = BuildSpecies(RoleType.Tank, default, new List<PassiveType>());

            var rolled = PassiveSystem.RollRandomPassive(species);

            Check(rolled == null, "EmptyPoolReturnsNull: pool vazio devolve null, não lança exceção");
        }

        private static void Test_IncompatiblePoolReturnsNull()
        {
            // Todo o pool é de outra Role - nenhuma entrada é compatível.
            var species = BuildSpecies(
                RoleType.Ranged,
                default,
                new List<PassiveType> { PassiveType.Couraca, PassiveType.Investida }
            );

            var rolled = PassiveSystem.RollRandomPassive(species);

            Check(rolled == null, "IncompatiblePoolReturnsNull: pool inteiro incompatível devolve null em vez de sortear algo errado");
        }

        private static void Test_RealSpeciesRollsFromPopulatedPool()
        {
            // Agumon (id=1, Warrior) tem pool real preenchido - confirma que o caminho real
            // (EggSystem/EnemyGenerator/Evolve) sorteia uma passiva de verdade, compatível
            // com a Role, sem lançar exceção.
            var agumon = DatabaseManager.Instance.GetDigimon(1);

            bool ok = true;
            PassiveType? rolled = null;

            try
            {
                rolled = PassiveSystem.RollRandomPassive(agumon);
            }
            catch
            {
                ok = false;
            }

            Check(
                ok && rolled.HasValue && agumon.Passives.Contains(rolled.Value),
                "RealSpeciesRollsFromPopulatedPool: Agumon sorteia uma passiva real do próprio pool, sem lançar exceção",
                $"(rolled={rolled}, pool={string.Join(",", agumon.Passives)})"
            );
        }

        private static void Test_NonexistentSpeciesDoesNotThrow()
        {
            // ID que não existe no banco - GetDigimon devolve null, e RollRandomPassive
            // precisa aceitar isso sem lançar exceção (ver PassiveSystem.RollRandomPassive).
            var missing = DatabaseManager.Instance.GetDigimon(999999);

            bool ok = true;
            PassiveType? rolled = null;

            try
            {
                rolled = PassiveSystem.RollRandomPassive(missing);
            }
            catch
            {
                ok = false;
            }

            Check(ok && rolled == null, "NonexistentSpeciesDoesNotThrow: espécie inexistente (null) devolve null sem lançar exceção");
        }

        private static void Test_EvolveRerollsForNewFormRole()
        {
            var fromForm = BuildSpecies(
                RoleType.Warrior,
                default,
                new List<PassiveType> { PassiveType.Investida }
            );

            var toForm = BuildSpecies(
                RoleType.Ranged,
                default,
                new List<PassiveType> { PassiveType.PosturaFirme, PassiveType.PesLeves }
            );

            toForm.Id = 99998;
            toForm.Code = "test_species_evolved";
            toForm.Name = "TestSpeciesEvolved";

            var digimon = new DigimonInstance(fromForm);

            digimon.PassiveId = PassiveSystem.RollRandomPassive(fromForm);

            Check(
                digimon.PassiveId == PassiveType.Investida,
                "EvolveRerollsForNewFormRole: passiva de nascimento veio do pool Warrior"
            );

            digimon.Evolve(toForm, 1.0f);

            bool rerolledToRangedPool =
                digimon.PassiveId == PassiveType.PosturaFirme ||
                digimon.PassiveId == PassiveType.PesLeves;

            Check(
                rerolledToRangedPool,
                "EvolveRerollsForNewFormRole: Evolve() re-sorteou pro pool Ranged da nova forma",
                $"(PassiveId={digimon.PassiveId})"
            );
        }

        /// <summary>Combatente descartável só pra teste - 100 de ATK físico e especial (não
        /// importa qual o AttackType do "atacante" de cada teste, sempre bate o suficiente),
        /// DEF baixa o bastante pra nunca cair no piso de dano mínimo 1 (o que mascararia o
        /// efeito da passiva sob teste).</summary>
        private static BattleCombatant BuildCombatant(
            RoleType role,
            AttackType attackType,
            int attack,
            int defense,
            PassiveType? passiveId = null)
        {
            var species = BuildSpecies(role, default, new List<PassiveType>());
            species.AttackType = attackType;
            species.BaseStats.PhysicalDamage = attack;
            species.BaseStats.SpecialDamage = attack;
            species.BaseStats.PhysicalDefense = defense;
            species.BaseStats.SpecialDefense = defense;

            var combatant = new BattleCombatant(new DigimonInstance(species));
            combatant.Digimon.PassiveId = passiveId;

            return combatant;
        }

        private static void ResetHealth(BattleCombatant combatant)
        {
            combatant.Digimon.CurrentHealthPoints = combatant.Digimon.MaxHealthPoints;
        }

        private static void Test_CouracaReducesFinalDamage()
        {
            var attacker = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20);
            var defenderNoPassive = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 20, defense: 20);
            var defenderWithCouraca = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 20, defense: 20, PassiveType.Couraca);

            const int trials = 4000;
            long totalNoPassive = 0;
            long totalWithPassive = 0;

            for (int i = 0; i < trials; i++)
            {
                ResetHealth(defenderNoPassive);
                ResetHealth(defenderWithCouraca);

                totalNoPassive += DamageCalculator.ResolveAttack(attacker, defenderNoPassive);
                totalWithPassive += DamageCalculator.ResolveAttack(attacker, defenderWithCouraca);
            }

            double ratio = (double)totalWithPassive / totalNoPassive;

            // Esperado ~0.82 (redução de 18%) - tolerância folgada por causa da variância
            // ±10% e do crítico base (10% de chance, x2), que afetam os dois lados igual e só
            // deixam Couraça divergir.
            Check(
                ratio > 0.75 && ratio < 0.89,
                "CouracaReducesFinalDamage: dano médio recebido cai ~18% com Couraça",
                $"(ratio={ratio:0.000}, esperado ~0.82)"
            );
        }

        private static void Test_PrecisaoLetalCritRate()
        {
            var attackerNoPassive = BuildCombatant(RoleType.Assassin, AttackType.Physical, attack: 100, defense: 20);
            var attackerWithPassive = BuildCombatant(RoleType.Assassin, AttackType.Physical, attack: 100, defense: 20, PassiveType.PrecisaoLetal);

            // Base antes de variância/crítico: 100 - 20 = 80. Não-crítico fica em [72,88]
            // (±10%); crítico (x2) fica em [144,176] - bem acima, então >120 (1.5x) separa
            // os dois grupos com folga.
            const float critThreshold = 80 * 1.5f;
            const int trials = 4000;

            double CritRate(BattleCombatant attacker)
            {
                var defender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);
                int crits = 0;

                for (int i = 0; i < trials; i++)
                {
                    ResetHealth(defender);

                    int dmg = DamageCalculator.ResolveAttack(attacker, defender);

                    if (dmg > critThreshold)
                        crits++;
                }

                return crits / (double)trials;
            }

            double noPassiveRate = CritRate(attackerNoPassive);
            double withPassiveRate = CritRate(attackerWithPassive);

            Check(
                noPassiveRate > 0.07 && noPassiveRate < 0.13,
                "PrecisaoLetalCritRate: taxa de crítico base fica ~10% sem a passiva",
                $"(taxa={noPassiveRate:0.000})"
            );

            Check(
                withPassiveRate > 0.19 && withPassiveRate < 0.25,
                "PrecisaoLetalCritRate: taxa de crítico sobe pra ~22% com Precisão Letal",
                $"(taxa={withPassiveRate:0.000})"
            );
        }

        private static void Test_FioDaLaminaCritMultiplier()
        {
            // PrecisaoLetal (22% de chance) só pra ter amostra suficiente de crítico em menos
            // tentativas - o que está sob teste aqui é o MULTIPLICADOR do Fio da Lâmina, não
            // a chance (já coberta no teste anterior), então usar as duas juntas não invalida
            // nada: Fio da Lâmina só define o multiplicador quando o crítico já rolou.
            var attackerBase = BuildCombatant(RoleType.Assassin, AttackType.Physical, attack: 100, defense: 20, PassiveType.PrecisaoLetal);
            var attackerFioDaLamina = BuildCombatant(RoleType.Assassin, AttackType.Physical, attack: 100, defense: 20, PassiveType.FioDaLamina);

            const float critThreshold = 80 * 1.5f;
            const int trials = 6000;

            double AverageCritDamage(BattleCombatant attacker)
            {
                var defender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);
                var critDamages = new List<int>();

                for (int i = 0; i < trials; i++)
                {
                    ResetHealth(defender);

                    int dmg = DamageCalculator.ResolveAttack(attacker, defender);

                    if (dmg > critThreshold)
                        critDamages.Add(dmg);
                }

                return critDamages.Count > 0 ? critDamages.Average() : 0;
            }

            double avgBase = AverageCritDamage(attackerBase);
            double avgFioDaLamina = AverageCritDamage(attackerFioDaLamina);

            double ratio = avgFioDaLamina / avgBase;

            // x3.2 / x2 = 1.6.
            Check(
                ratio > 1.45 && ratio < 1.75,
                "FioDaLaminaCritMultiplier: dano médio de crítico ~1.6x maior que o crítico base (x3.2 vs x2)",
                $"(ratio={ratio:0.000}, esperado ~1.6)"
            );
        }

        private static void Test_RompeGuardaDamageGainCapped()
        {
            // DEF=85 pra 100 de ATK - mesmo cenário citado na spec (perto do teto de defesa
            // de 85% do EnemyGenerator) onde o ganho sem teto seria desproporcional.
            var attackerNoPassive = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20);
            var attackerRompeGuarda = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20, PassiveType.RompeGuarda);
            var defender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 85);

            const int trials = 4000;
            long totalNoPassive = 0;
            long totalRompeGuarda = 0;

            for (int i = 0; i < trials; i++)
            {
                ResetHealth(defender);
                totalNoPassive += DamageCalculator.ResolveAttack(attackerNoPassive, defender);

                ResetHealth(defender);
                totalRompeGuarda += DamageCalculator.ResolveAttack(attackerRompeGuarda, defender);
            }

            double ratio = (double)totalRompeGuarda / totalNoPassive;

            // Base = 100-85 = 15. Pierce (25%) = DEF efetiva 63 -> dano 37 (2.47x, MUITO acima
            // do teto). Com o teto de +40% do dano base, o esperado é 15+6=21 (1.4x). Se o
            // teto não estivesse aplicado, essa checagem pegaria a diferença (ratio > 2.0).
            Check(
                ratio > 1.25 && ratio < 1.55,
                "RompeGuardaDamageGainCapped: ganho fica em ~+40% do dano base (teto ativo), não ~+147% (pierce sem teto)",
                $"(ratio={ratio:0.000}, esperado ~1.4)"
            );
        }

        private static void Test_FuriaCrescenteStacksAndDecays()
        {
            var attacker = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20, PassiveType.FuriaCrescente);
            var defender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);

            for (int i = 0; i < 3; i++)
            {
                ResetHealth(defender);
                DamageCalculator.ResolveAttack(attacker, defender);
            }

            Check(
                attacker.FuriaCrescenteStacks == 3,
                "FuriaCrescenteStacksAndDecays: 3 golpes seguidos acumulam 3 pilhas",
                $"(stacks={attacker.FuriaCrescenteStacks})"
            );

            for (int i = 0; i < 5; i++)
            {
                ResetHealth(defender);
                DamageCalculator.ResolveAttack(attacker, defender);
            }

            Check(
                attacker.FuriaCrescenteStacks == 5,
                "FuriaCrescenteStacksAndDecays: teto de 5 pilhas respeitado (8 golpes seguidos não passam disso)",
                $"(stacks={attacker.FuriaCrescenteStacks})"
            );

            // Simula 3s+ sem atacar (mesma condição que BattleCombatant.Tick decrementaria).
            attacker.FuriaCrescenteDecayRemainingSeconds = -1;
            ResetHealth(defender);
            DamageCalculator.ResolveAttack(attacker, defender);

            Check(
                attacker.FuriaCrescenteStacks == 1,
                "FuriaCrescenteStacksAndDecays: pilhas zeram depois de 3s sem atacar, próximo golpe recomeça em 1",
                $"(stacks={attacker.FuriaCrescenteStacks})"
            );
        }

        private static void Test_GolpePesadoCounterAndBonus()
        {
            var attacker = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20, PassiveType.GolpePesado);
            var defender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);

            for (int i = 0; i < 3; i++)
            {
                ResetHealth(defender);
                DamageCalculator.ResolveAttack(attacker, defender);
            }

            Check(
                attacker.GolpePesadoHitCounter == 3,
                "GolpePesadoCounterAndBonus: contador chega a 3 depois de 3 golpes",
                $"(counter={attacker.GolpePesadoHitCounter})"
            );

            ResetHealth(defender);
            DamageCalculator.ResolveAttack(attacker, defender);

            Check(
                attacker.GolpePesadoHitCounter == 0,
                "GolpePesadoCounterAndBonus: contador zera no 4º golpe (proc)",
                $"(counter={attacker.GolpePesadoHitCounter})"
            );

            // Compara a média dos golpes 1-3 (sem bônus) com a média só dos 4os golpes (com
            // bônus) ao longo de várias rodadas - esperado ~1.5x.
            const int rounds = 1500;
            long totalNormal = 0;
            long totalBonus = 0;

            for (int r = 0; r < rounds; r++)
            {
                for (int i = 0; i < 3; i++)
                {
                    ResetHealth(defender);
                    totalNormal += DamageCalculator.ResolveAttack(attacker, defender);
                }

                ResetHealth(defender);
                totalBonus += DamageCalculator.ResolveAttack(attacker, defender);
            }

            double avgNormal = totalNormal / (double)(rounds * 3);
            double avgBonus = totalBonus / (double)rounds;
            double ratio = avgBonus / avgNormal;

            Check(
                ratio > 1.35 && ratio < 1.65,
                "GolpePesadoCounterAndBonus: dano do 4º golpe ~1.5x maior que os outros 3",
                $"(ratio={ratio:0.000}, esperado ~1.5)"
            );
        }

        private static void Test_ExecucaoBonusAgainstLowHp()
        {
            var attacker = BuildCombatant(RoleType.Assassin, AttackType.Physical, attack: 100, defense: 20, PassiveType.Execucao);
            var defenderHealthy = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);
            var defenderLowHp = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);

            const int trials = 3000;
            long totalHealthy = 0;
            long totalLowHp = 0;

            for (int i = 0; i < trials; i++)
            {
                ResetHealth(defenderHealthy);
                totalHealthy += DamageCalculator.ResolveAttack(attacker, defenderHealthy);

                defenderLowHp.Digimon.CurrentHealthPoints = (int)(defenderLowHp.Digimon.MaxHealthPoints * 0.35f);
                totalLowHp += DamageCalculator.ResolveAttack(attacker, defenderLowHp);
            }

            double ratio = (double)totalLowHp / totalHealthy;

            Check(
                ratio > 1.15 && ratio < 1.35,
                "ExecucaoBonusAgainstLowHp: dano ~25% maior contra alvo abaixo de 40% de HP",
                $"(ratio={ratio:0.000}, esperado ~1.25)"
            );
        }

        private static void Test_GolpeSorrateiroOnlyFirstHit()
        {
            var attacker = BuildCombatant(RoleType.Assassin, AttackType.Physical, attack: 100, defense: 20, PassiveType.GolpeSorrateiro);
            var defender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);

            const int trials = 3000;
            long totalFirstHit = 0;
            long totalSecondHit = 0;

            for (int i = 0; i < trials; i++)
            {
                // Combatente novo a cada rodada - GolpeSorrateiroHitTargets é por par
                // atacante/alvo, então precisa de um alvo "nunca visto" pra medir o 1º golpe.
                var freshDefender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);

                totalFirstHit += DamageCalculator.ResolveAttack(attacker, freshDefender);

                ResetHealth(freshDefender);
                totalSecondHit += DamageCalculator.ResolveAttack(attacker, freshDefender);
            }

            double ratio = (double)totalFirstHit / totalSecondHit;

            Check(
                ratio > 1.65 && ratio < 1.95,
                "GolpeSorrateiroOnlyFirstHit: 1º golpe contra um alvo ~1.8x maior que o 2º contra o mesmo alvo",
                $"(ratio={ratio:0.000}, esperado ~1.8)"
            );

            Check(
                attacker.GolpeSorrateiroHitTargets.Contains(defender) == false,
                "GolpeSorrateiroOnlyFirstHit: alvo usado só na configuração (nunca atingido) não fica marcado"
            );
        }

        private static void Test_AlvoMarcadoAppliesSpeedDebuff()
        {
            var attacker = BuildCombatant(RoleType.Ranged, AttackType.Physical, attack: 100, defense: 20, PassiveType.AlvoMarcado);
            var defender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);

            for (int i = 0; i < 3; i++)
            {
                ResetHealth(defender);
                DamageCalculator.ResolveAttack(attacker, defender);
            }

            bool noEffectYet = !defender.Effects.Any(e => e.Stat == StatType.Speed);

            Check(noEffectYet, "AlvoMarcadoAppliesSpeedDebuff: sem debuff antes do 4º acerto");

            ResetHealth(defender);
            DamageCalculator.ResolveAttack(attacker, defender);

            var speedEffect = defender.Effects.FirstOrDefault(e => e.Stat == StatType.Speed);

            Check(
                speedEffect != null && speedEffect.Percentage < 0,
                "AlvoMarcadoAppliesSpeedDebuff: 4º acerto aplica debuff de Speed no alvo",
                speedEffect == null ? "(nenhum efeito encontrado)" : $"(percentage={speedEffect.Percentage})"
            );
        }

        private static void Test_UltimaTrincheiraTriggersOnceBelowThreshold()
        {
            var attacker = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20);
            var defender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20, PassiveType.UltimaTrincheira);

            defender.Digimon.CurrentHealthPoints = (int)(defender.Digimon.MaxHealthPoints * 0.35f);

            Check(
                !defender.UltimaTrincheiraTriggered,
                "UltimaTrincheiraTriggersOnceBelowThreshold: ainda não disparou antes de tomar dano"
            );

            DamageCalculator.ResolveAttack(attacker, defender);

            Check(
                defender.UltimaTrincheiraTriggered && defender.UltimaTrincheiraRemainingSeconds > 0,
                "UltimaTrincheiraTriggersOnceBelowThreshold: dispara ao tomar dano já abaixo de 40% de HP",
                $"(triggered={defender.UltimaTrincheiraTriggered}, remaining={defender.UltimaTrincheiraRemainingSeconds})"
            );

            // Compara dano recebido com a janela ativa (defenderComTrincheira) vs sem a
            // passiva, ambos no mesmo HP baixo - esperado ~0.5x.
            var defenderNoPassive = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);

            const int trials = 3000;
            long totalWithTrincheira = 0;
            long totalNoPassive = 0;

            for (int i = 0; i < trials; i++)
            {
                defender.Digimon.CurrentHealthPoints = (int)(defender.Digimon.MaxHealthPoints * 0.35f);
                defenderNoPassive.Digimon.CurrentHealthPoints = (int)(defenderNoPassive.Digimon.MaxHealthPoints * 0.35f);

                totalWithTrincheira += DamageCalculator.ResolveAttack(attacker, defender);
                totalNoPassive += DamageCalculator.ResolveAttack(attacker, defenderNoPassive);
            }

            double ratio = (double)totalWithTrincheira / totalNoPassive;

            Check(
                ratio > 0.42 && ratio < 0.58,
                "UltimaTrincheiraTriggersOnceBelowThreshold: dano recebido cai ~50% com a janela ativa",
                $"(ratio={ratio:0.000}, esperado ~0.5)"
            );
        }

        private static void Test_TeimosiaScalesWithMissingHp()
        {
            var attacker = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20);
            var defenderFullHp = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 0, defense: 20, PassiveType.Teimosia);
            var defenderLowHp = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 0, defense: 20, PassiveType.Teimosia);

            const int trials = 3000;
            long totalFullHp = 0;
            long totalLowHp = 0;

            for (int i = 0; i < trials; i++)
            {
                defenderFullHp.Digimon.CurrentHealthPoints = defenderFullHp.Digimon.MaxHealthPoints;
                defenderLowHp.Digimon.CurrentHealthPoints = (int)(defenderLowHp.Digimon.MaxHealthPoints * 0.1f);

                totalFullHp += DamageCalculator.ResolveAttack(attacker, defenderFullHp);
                totalLowHp += DamageCalculator.ResolveAttack(attacker, defenderLowHp);
            }

            double ratio = (double)totalLowHp / totalFullHp;

            // Esperado (1 - 0.30*(1-0.1)) / (1 - 0.30*(1-1.0)) = 0.73 / 1.0 = 0.73.
            Check(
                ratio > 0.65 && ratio < 0.81,
                "TeimosiaScalesWithMissingHp: dano recebido com HP quase no mínimo cai ~27% a mais que com HP cheio",
                $"(ratio={ratio:0.000}, esperado ~0.73)"
            );
        }

        private static void Test_EspinhosReflectsToMeleeOnly()
        {
            var defender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20, PassiveType.Espinhos);

            var meleeAttacker = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20);
            var rangedAttacker = BuildCombatant(RoleType.Ranged, AttackType.Physical, attack: 100, defense: 20);

            ResetHealth(defender);
            int meleeAttackerHpBefore = meleeAttacker.Digimon.CurrentHealthPoints;
            DamageCalculator.ResolveAttack(meleeAttacker, defender);
            int meleeAttackerHpAfter = meleeAttacker.Digimon.CurrentHealthPoints;

            Check(
                meleeAttackerHpAfter < meleeAttackerHpBefore,
                "EspinhosReflectsToMeleeOnly: atacante melee toma dano refletido",
                $"(antes={meleeAttackerHpBefore}, depois={meleeAttackerHpAfter})"
            );

            ResetHealth(defender);
            int rangedAttackerHpBefore = rangedAttacker.Digimon.CurrentHealthPoints;
            DamageCalculator.ResolveAttack(rangedAttacker, defender);
            int rangedAttackerHpAfter = rangedAttacker.Digimon.CurrentHealthPoints;

            Check(
                rangedAttackerHpAfter == rangedAttackerHpBefore,
                "EspinhosReflectsToMeleeOnly: atacante Ranged NÃO toma dano refletido",
                $"(antes={rangedAttackerHpBefore}, depois={rangedAttackerHpAfter})"
            );
        }

        private static void Test_InvestidaConsumesFlagOnce()
        {
            var attacker = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20, PassiveType.Investida);
            var defender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);

            const int trials = 3000;
            long totalNoBonus = 0;
            long totalWithBonus = 0;

            for (int i = 0; i < trials; i++)
            {
                ResetHealth(defender);
                attacker.InvestidaPendingBonus = false;
                totalNoBonus += DamageCalculator.ResolveAttack(attacker, defender);

                ResetHealth(defender);
                attacker.InvestidaPendingBonus = true;
                totalWithBonus += DamageCalculator.ResolveAttack(attacker, defender);
            }

            double ratio = (double)totalWithBonus / totalNoBonus;

            Check(
                ratio > 1.45 && ratio < 1.75,
                "InvestidaConsumesFlagOnce: golpe com a flag ativa causa ~1.6x mais dano",
                $"(ratio={ratio:0.000}, esperado ~1.6)"
            );

            Check(
                attacker.InvestidaPendingBonus == false,
                "InvestidaConsumesFlagOnce: flag é consumida (false) depois de um golpe que a usou"
            );
        }

        private static void Test_PosturaFirmeBonusDamage()
        {
            var attackerNoPassive = BuildCombatant(RoleType.Ranged, AttackType.Physical, attack: 100, defense: 20);
            var attackerPosturaFirme = BuildCombatant(RoleType.Ranged, AttackType.Physical, attack: 100, defense: 20, PassiveType.PosturaFirme);
            var defender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);

            const int trials = 4000;
            long totalNoPassive = 0;
            long totalPosturaFirme = 0;

            for (int i = 0; i < trials; i++)
            {
                ResetHealth(defender);
                totalNoPassive += DamageCalculator.ResolveAttack(attackerNoPassive, defender);

                ResetHealth(defender);
                totalPosturaFirme += DamageCalculator.ResolveAttack(attackerPosturaFirme, defender);
            }

            double ratio = (double)totalPosturaFirme / totalNoPassive;

            Check(
                ratio > 1.15 && ratio < 1.35,
                "PosturaFirmeBonusDamage: dano final ~25% maior",
                $"(ratio={ratio:0.000}, esperado ~1.25)"
            );
        }

        private static void Test_FixacaoConsecutiveHitsIncrement()
        {
            var attacker = BuildCombatant(RoleType.Ranged, AttackType.Physical, attack: 100, defense: 20, PassiveType.Fixacao);
            var defender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);

            Check(attacker.FixacaoConsecutiveHits == 0, "FixacaoConsecutiveHitsIncrement: começa em 0");

            for (int i = 0; i < 5; i++)
            {
                ResetHealth(defender);
                DamageCalculator.ResolveAttack(attacker, defender);
            }

            Check(
                attacker.FixacaoConsecutiveHits == 5,
                "FixacaoConsecutiveHitsIncrement: incrementa a cada golpe confirmado (sem teto aqui - o teto de 4 pilhas é aplicado em BattleUnit.ComputeEffectiveCooldown, não no contador)",
                $"(hits={attacker.FixacaoConsecutiveHits})"
            );
        }

        private static void Test_GritoDeGuerraReducesDamageWhenTaunted()
        {
            var attacker = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20);
            var tank = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20, PassiveType.GritoDeGuerra);
            var otherTank = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20, PassiveType.GritoDeGuerra);

            const int trials = 4000;
            long totalNotTaunted = 0;
            long totalTauntedByThisTank = 0;

            for (int i = 0; i < trials; i++)
            {
                attacker.TauntedBy = null;
                ResetHealth(tank);
                totalNotTaunted += DamageCalculator.ResolveAttack(attacker, tank);

                attacker.TauntedBy = tank;
                ResetHealth(tank);
                totalTauntedByThisTank += DamageCalculator.ResolveAttack(attacker, tank);
            }

            double ratio = (double)totalTauntedByThisTank / totalNotTaunted;

            Check(
                ratio > 0.62 && ratio < 0.78,
                "GritoDeGuerraReducesDamageWhenTaunted: dano cai ~30% quando o atacante está sob o Taunt DESSE Tank",
                $"(ratio={ratio:0.000}, esperado ~0.7)"
            );

            // Provocado por OUTRO Tank (com a mesma passiva) não deveria reduzir o dano
            // contra ESTE Tank - TauntedBy precisa apontar pro defender específico. Média
            // sobre várias tentativas, não uma amostra só - um único golpe tem variância
            // ±10% (e 10% de chance de crítico) por conta própria, então comparar UMA
            // amostra contra um limiar fixo é inerentemente instável mesmo sem bug nenhum.
            long totalTauntedByOther = 0;

            for (int i = 0; i < trials; i++)
            {
                attacker.TauntedBy = otherTank;
                ResetHealth(tank);
                totalTauntedByOther += DamageCalculator.ResolveAttack(attacker, tank);
            }

            double ratioOther = (double)totalTauntedByOther / totalNotTaunted;

            Check(
                ratioOther > 0.9 && ratioOther < 1.1,
                "GritoDeGuerraReducesDamageWhenTaunted: Taunt de OUTRO Tank não reduz dano contra este",
                $"(ratio={ratioOther:0.000}, esperado ~1.0)"
            );
        }

        private static void Test_RessonanciaStackingCurve()
        {
            var target = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20);

            // Simula exatamente o que BattleUnit.ApplyRessonanciaBuff faz: consulta quantas
            // pilhas de ATK já existem e escolhe o percentual certo antes de cada AddEffect.
            for (int i = 0; i < 3; i++)
            {
                int existingStacks = target.Effects.Count(e => e.Stat == target.AttackStat && e.Percentage > 0);
                float percentage = existingStacks >= 2 ? 0.10f : 0.20f;

                target.AddEffect(target.AttackStat, percentage, 6.0, maxStacksOverride: 3);
            }

            var atkEffects = target.Effects.Where(e => e.Stat == target.AttackStat && e.Percentage > 0).ToList();

            Check(
                atkEffects.Count == 3,
                "RessonanciaStackingCurve: chega a 3 pilhas (acima do teto padrão de 2)",
                $"(pilhas={atkEffects.Count})"
            );

            double totalPercentage = atkEffects.Sum(e => e.Percentage);

            Check(
                Math.Abs(totalPercentage - 0.50) < 0.001,
                "RessonanciaStackingCurve: soma das pilhas é 20% + 20% + 10% = 50%",
                $"(soma={totalPercentage})"
            );

            // Uma 4ª tentativa não deveria criar uma 4ª pilha.
            target.AddEffect(target.AttackStat, 0.10f, 6.0, maxStacksOverride: 3);

            int countAfterFourth = target.Effects.Count(e => e.Stat == target.AttackStat && e.Percentage > 0);

            Check(
                countAfterFourth == 3,
                "RessonanciaStackingCurve: teto de 3 respeitado numa 4ª tentativa",
                $"(pilhas={countAfterFourth})"
            );
        }

        private static void Test_ShieldAbsorbsDamageFully()
        {
            var attacker = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20);
            var defender = BuildCombatant(RoleType.Support, AttackType.Physical, attack: 0, defense: 20);

            defender.ShieldAmount = 100000;

            int hpBefore = defender.Digimon.CurrentHealthPoints;

            DamageCalculator.ResolveAttack(attacker, defender);

            Check(
                defender.Digimon.CurrentHealthPoints == hpBefore,
                "ShieldAbsorbsDamageFully: escudo grande o bastante absorve o golpe inteiro, HP intacto",
                $"(HP antes={hpBefore}, depois={defender.Digimon.CurrentHealthPoints})"
            );

            Check(
                defender.ShieldAmount < 100000,
                "ShieldAbsorbsDamageFully: escudo é consumido pelo valor do dano absorvido",
                $"(escudo restante={defender.ShieldAmount})"
            );
        }

        private static void Test_ShieldPartialAbsorptionThenHp()
        {
            var attacker = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20);
            var defender = BuildCombatant(RoleType.Support, AttackType.Physical, attack: 0, defense: 20);

            // HP bem acima do maior dano possível desse ataque (mesmo com crítico) - senão
            // um crítico raro (10% de chance, x2) pode matar o defender de um golpe só e
            // TakeDamage clampar a perda de HP no que sobrava, quebrando a igualdade exata
            // que este teste verifica (não é bug de produção, é só o defender de teste
            // precisar de mais fôlego).
            defender.Digimon.MaxHealthPoints = 100000;
            defender.Digimon.CurrentHealthPoints = 100000;

            defender.ShieldAmount = 10;
            ResetHealth(defender);
            int hpBefore = defender.Digimon.CurrentHealthPoints;

            int damage = DamageCalculator.ResolveAttack(attacker, defender);
            int hpLost = hpBefore - defender.Digimon.CurrentHealthPoints;

            Check(
                defender.ShieldAmount == 0,
                "ShieldPartialAbsorptionThenHp: escudo pequeno é totalmente consumido",
                $"(escudo restante={defender.ShieldAmount})"
            );

            Check(
                hpLost == damage - 10,
                "ShieldPartialAbsorptionThenHp: só o excedente (dano - escudo) vai pro HP",
                $"(dano={damage}, HP perdido={hpLost}, esperado={damage - 10})"
            );

            // Sem escudo agora - o próximo golpe deveria ir inteiro pro HP.
            ResetHealth(defender);
            hpBefore = defender.Digimon.CurrentHealthPoints;
            int damage2 = DamageCalculator.ResolveAttack(attacker, defender);
            int hpLost2 = hpBefore - defender.Digimon.CurrentHealthPoints;

            Check(
                hpLost2 == damage2,
                "ShieldPartialAbsorptionThenHp: sem escudo, o próximo golpe vai inteiro pro HP",
                $"(dano={damage2}, HP perdido={hpLost2})"
            );
        }

        private static void Test_ShieldDoesNotAffectEspinhosReflection()
        {
            var attacker = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20);
            var defenderNoShield = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20, PassiveType.Espinhos);
            var defenderWithShield = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20, PassiveType.Espinhos);

            defenderWithShield.ShieldAmount = 100000;

            const int trials = 3000;
            long totalReflectedNoShield = 0;
            long totalReflectedWithShield = 0;

            for (int i = 0; i < trials; i++)
            {
                ResetHealth(attacker);
                ResetHealth(defenderNoShield);
                int before = attacker.Digimon.CurrentHealthPoints;
                DamageCalculator.ResolveAttack(attacker, defenderNoShield);
                totalReflectedNoShield += before - attacker.Digimon.CurrentHealthPoints;

                ResetHealth(attacker);
                before = attacker.Digimon.CurrentHealthPoints;
                DamageCalculator.ResolveAttack(attacker, defenderWithShield);
                totalReflectedWithShield += before - attacker.Digimon.CurrentHealthPoints;
            }

            double ratio = (double)totalReflectedWithShield / totalReflectedNoShield;

            Check(
                ratio > 0.85 && ratio < 1.15,
                "ShieldDoesNotAffectEspinhosReflection: Espinhos reflete igual, com ou sem o escudo absorvendo o golpe original",
                $"(ratio={ratio:0.000}, esperado ~1.0)"
            );
        }

        private static void Test_HealEffectivenessMultiplierCurve()
        {
            float atStart = BattleCombatant.GetHealEffectivenessMultiplier(0);
            float atGraceEnd = BattleCombatant.GetHealEffectivenessMultiplier(30);
            float justAfterGrace = BattleCombatant.GetHealEffectivenessMultiplier(31);
            float veryLate = BattleCombatant.GetHealEffectivenessMultiplier(1000);

            Check(Math.Abs(atStart - 1f) < 0.001f, "HealEffectivenessMultiplierCurve: 100% no início da luta", $"(veio {atStart})");
            Check(Math.Abs(atGraceEnd - 1f) < 0.001f, "HealEffectivenessMultiplierCurve: ainda 100% aos 30s (fim da graça)", $"(veio {atGraceEnd})");
            Check(Math.Abs(justAfterGrace - 0.85f) < 0.001f, "HealEffectivenessMultiplierCurve: 85% logo após a graça (1º degrau de -15%)", $"(veio {justAfterGrace})");
            Check(Math.Abs(veryLate - 0.2f) < 0.001f, "HealEffectivenessMultiplierCurve: nunca cai abaixo do piso de 20%", $"(veio {veryLate})");
        }

        private static void Test_FolegoRegeneratesOverTime()
        {
            var tank = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20, PassiveType.Folego);
            tank.Digimon.CurrentHealthPoints = (int)(tank.Digimon.MaxHealthPoints * 0.5f);
            int hpBefore = tank.Digimon.CurrentHealthPoints;

            const double totalSeconds = 10.0;
            const double step = 1.0 / 60.0;
            double elapsed = 0;

            while (elapsed < totalSeconds)
            {
                tank.Tick(step, elapsed);
                elapsed += step;
            }

            int hpGained = tank.Digimon.CurrentHealthPoints - hpBefore;
            int expected = (int)(tank.Digimon.MaxHealthPoints * 0.006f * totalSeconds);

            Check(
                Math.Abs(hpGained - expected) <= 1,
                "FolegoRegeneratesOverTime: regenera ~0.6%/s do HP máximo (10s de luta, dentro da graça de R2)",
                $"(ganho={hpGained}, esperado~{expected})"
            );
        }

        private static void Test_FolegoDoesNotExceedMaxHp()
        {
            var tank = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20, PassiveType.Folego);
            ResetHealth(tank);

            tank.Tick(5.0, 0);

            Check(
                tank.Digimon.CurrentHealthPoints == tank.Digimon.MaxHealthPoints,
                "FolegoDoesNotExceedMaxHp: não regenera acima do HP máximo"
            );
        }

        private static void Test_SedeDeBatalhaHealsAttacker()
        {
            var attacker = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20, PassiveType.SedeDeBatalha);
            var defender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);

            // Bastante espaço de HP pra curar sem bater no teto do HP máximo.
            attacker.Digimon.MaxHealthPoints = 100000;

            const int trials = 3000;
            long totalDamageDealt = 0;
            long totalHealed = 0;

            for (int i = 0; i < trials; i++)
            {
                ResetHealth(defender);
                attacker.Digimon.CurrentHealthPoints = 1;

                int damage = DamageCalculator.ResolveAttack(attacker, defender);

                totalDamageDealt += damage;
                totalHealed += attacker.Digimon.CurrentHealthPoints - 1;
            }

            double ratio = (double)totalHealed / totalDamageDealt;

            Check(
                ratio > 0.17 && ratio < 0.23,
                "SedeDeBatalhaHealsAttacker: cura ~20% do dano final causado",
                $"(ratio={ratio:0.000}, esperado ~0.20)"
            );
        }

        private static void Test_SedeDeBatalhaRespectsR2Decay()
        {
            var attacker = BuildCombatant(RoleType.Warrior, AttackType.Physical, attack: 100, defense: 20, PassiveType.SedeDeBatalha);
            var defender = BuildCombatant(RoleType.Tank, AttackType.Physical, attack: 0, defense: 20);

            attacker.Digimon.MaxHealthPoints = 100000;

            const int trials = 3000;
            long totalDamageEarly = 0, totalHealedEarly = 0;
            long totalDamageLate = 0, totalHealedLate = 0;

            for (int i = 0; i < trials; i++)
            {
                ResetHealth(defender);
                attacker.Digimon.CurrentHealthPoints = 1;
                int dmg = DamageCalculator.ResolveAttack(attacker, defender, elapsedSeconds: 0);
                totalDamageEarly += dmg;
                totalHealedEarly += attacker.Digimon.CurrentHealthPoints - 1;

                ResetHealth(defender);
                attacker.Digimon.CurrentHealthPoints = 1;
                dmg = DamageCalculator.ResolveAttack(attacker, defender, elapsedSeconds: 1000);
                totalDamageLate += dmg;
                totalHealedLate += attacker.Digimon.CurrentHealthPoints - 1;
            }

            double ratioEarly = (double)totalHealedEarly / totalDamageEarly;
            double ratioLate = (double)totalHealedLate / totalDamageLate;

            Check(
                ratioEarly > 0.17 && ratioEarly < 0.23,
                "SedeDeBatalhaRespectsR2Decay: ~20% de cura no início da luta (100% de eficácia)",
                $"(ratio={ratioEarly:0.000})"
            );

            // Piso de eficácia R2 é 20%, então 20% de cura x 20% de eficácia = 4%.
            Check(
                ratioLate > 0.03 && ratioLate < 0.05,
                "SedeDeBatalhaRespectsR2Decay: cai pro piso (~4% de cura) numa luta muito longa",
                $"(ratio={ratioLate:0.000})"
            );
        }
    }
}
