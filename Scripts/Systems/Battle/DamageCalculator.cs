using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Managers;
using System;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>
    /// Fórmula de dano da batalha (extraída do antigo BattleSystem 1x1, sem alteração de
    /// comportamento): ATK - DEF do alvo (mínimo 1), multiplicador de vantagem de tipo
    /// (ver <see cref="TypeAdvantageCalculator"/>), variação aleatória ±10%, crítico com
    /// 10% de chance dobra o dano. Lê os stats via <see cref="BattleCombatant.GetEffectiveStat"/>,
    /// então buffs/debuffs ativos já entram na conta.
    /// </summary>
    public static class DamageCalculator
    {
        private static readonly Random _random = new();

        // Rompe-Guarda (Warrior, ver PASSIVAS_SPEC.md): ignora uma fração da DEF efetiva do
        // alvo, mas o ganho (dano a mais em relação ao dano base SEM ignorar DEF) é limitado
        // a essa fração do próprio dano base - sem o teto, contra um inimigo no teto de
        // defesa do EnemyGenerator (85% do ataque médio), ignorar DEF geraria um multiplicador
        // muito maior que qualquer outra passiva ofensiva da lista.
        private const float RompeGuardaDefensePierce = 0.25f;
        private const float RompeGuardaMaxGainRatio = 0.40f;

        // Couraça (Tank): reduz o dano final recebido - aplicado no fim do pipeline, igual a
        // toda passiva ofensiva/defensiva de dano (ver R1 da spec: nunca em % de ATK/DEF).
        private const float CouracaDamageReduction = 0.18f;

        private const int BaseCritChancePercent = 10;
        private const int PrecisaoLetalCritChancePercent = 22;
        private const float BaseCritMultiplier = 2f;
        private const float FioDaLaminaCritMultiplier = 3.2f;

        // Fúria Crescente (Warrior): +X% de dano final por golpe acertado, até um teto de
        // pilhas, decaindo se passar tempo demais sem atacar de novo (ver BattleCombatant.
        // FuriaCrescenteDecayRemainingSeconds/Tick).
        private const float FuriaCrescenteStackBonus = 0.05f;
        private const int FuriaCrescenteMaxStacks = 5;
        private const double FuriaCrescenteDecayWindow = 3.0;

        // Golpe Pesado (Warrior): a cada N golpes, dano bonificado - o splash em área fica em
        // BattleUnit.ApplyAction (só quem tem acesso a posição/time inimigo), que lê
        // GolpePesadoHitCounter == 0 logo depois do ResolveAttack pra saber se esse golpe
        // específico foi o que acabou de disparar o bônus.
        private const int GolpePesadoHitInterval = 4;
        private const float GolpePesadoBonusDamage = 0.50f;

        // Execução (Assassin): bônus fixo contra alvo já ferido - sem estado nenhum, só lê
        // BattleCombatant.HpPercentage do alvo ANTES desse golpe.
        private const float ExecucaoBonusDamage = 0.25f;
        private const float ExecucaoHpThreshold = 0.40f;

        // Golpe Sorrateiro (Assassin): bônus só no primeiro golpe contra CADA alvo na luta -
        // o limite por alvo (GolpeSorrateiroHitTargets) é essencial, ver a spec.
        private const float GolpeSorrateiroBonusDamage = 0.80f;

        // Alvo Marcado (Ranged): a cada N acertos, debuff de Speed no alvo - reaproveita
        // BattleCombatant.AddEffect (mesmo sistema de buff/debuff dos Support), não precisa
        // de timer próprio.
        private const int AlvoMarcadoHitInterval = 4;
        private const float AlvoMarcadoSpeedDebuff = -0.12f;
        private const double AlvoMarcadoDebuffDuration = 5.0;

        // Última Trincheira (Tank): abaixo do limiar de HP, ativa uma janela de redução de
        // dano - só uma vez por luta (UltimaTrincheiraTriggered).
        private const float UltimaTrincheiraHpThreshold = 0.40f;
        private const float UltimaTrincheiraDamageReduction = 0.50f;
        private const double UltimaTrincheiraDuration = 5.0;

        // Teimosia (Warrior): redução de dano final que escala linearmente com o HP perdido -
        // 0% com HP cheio, até esse teto com HP no mínimo.
        private const float TeimosiaMaxReduction = 0.30f;

        // Grito de Guerra (Tank): quem está sob o Taunt DESSE Tank causa menos dano enquanto
        // durar - attacker.TauntedBy (escrito por BattleUnit) precisa apontar pra ESSE
        // defender especificamente, não só "estar sob Taunt de alguém".
        private const float GritoDeGuerraDamageReduction = 0.30f;

        // Espinhos (Tank): devolve uma fração do dano final recebido - só contra ataques
        // corpo a corpo (Ranged não conta), e via TakeDamage direto (nunca reentra em
        // ResolveAttack), pra nunca disparar Espinhos do outro lado.
        private const float EspinhosReflectPercentage = 0.15f;

        // Investida (Warrior): bônus no primeiro golpe depois de um dash - a decisão de
        // QUANDO a flag liga (ao disparar o dash) é do BattleUnit (só ele sabe de
        // movimento); aqui só consome.
        private const float InvestidaDashBonusDamage = 0.60f;

        // Postura Firme (Ranged): dano final fixo maior, compensando abrir mão de
        // órbita/fuga (ver BattleUnit.ComputeStandGroundPosition).
        private const float PosturaFirmeBonusDamage = 0.25f;

        // Sede de Batalha (Warrior): cura o atacante por uma fração do dano final CAUSADO
        // (por isso é aplicada depois de ApplyDefenderPassives, não em ApplyAttackerPassives -
        // "dano final causado" é o número depois de tudo, inclusive reduções do lado do
        // defensor, não só o que o atacante já tinha calculado antes disso). Obedece o
        // decaimento R2 (BattleCombatant.GetHealEffectivenessMultiplier) - precisa do tempo
        // decorrido da luta, por isso ResolveAttack ganhou o parâmetro elapsedSeconds.
        private const float SedeDeBatalhaHealPercentage = 0.20f;

        private static readonly HashSet<RoleType> MeleeRoles = new()
        {
            RoleType.Warrior,
            RoleType.Tank,
            RoleType.Assassin
        };

        /// <summary>Overload sem tempo de luta - usado por quem não se importa com o
        /// decaimento R2 (ex.: testes). Equivale a chamar com elapsedSeconds=0 (100% de
        /// eficácia de cura/escudo, como se fosse o início da luta).</summary>
        public static int ResolveAttack(BattleCombatant attacker, BattleCombatant defender)
        {
            return ResolveAttack(attacker, defender, elapsedSeconds: 0);
        }

        public static int ResolveAttack(BattleCombatant attacker, BattleCombatant defender, double elapsedSeconds)
        {
            int damage = CalculateBaseDamage(attacker, defender);

            damage = ApplyTypeAdvantage(damage, attacker, defender);
            damage = ApplyDamageVariance(damage);
            damage = ApplyCritical(damage, attacker);
            damage = ApplyAttackerPassives(damage, attacker, defender);
            damage = ApplySetBonuses(damage, attacker);
            damage = ApplyDefenderPassives(damage, attacker, defender);

            ApplySedeDeBatalha(attacker, damage, elapsedSeconds);

            // Vigília (Healer, ver PASSIVAS_SPEC.md): escudo absorve ANTES do HP. Espinhos
            // (acima, em ApplyDefenderPassives) reflete com base no dano final "de verdade",
            // não no que sobrou depois do escudo - o escudo é um recurso à parte, não uma
            // redução de dano.
            int hpDamage = ConsumeShield(damage, defender);

            defender.Digimon.TakeDamage(hpDamage);

            return damage;
        }

        /// <summary>Sede de Batalha (Warrior): cura o atacante com base no dano final
        /// causado, já com o decaimento R2 aplicado.</summary>
        private static void ApplySedeDeBatalha(BattleCombatant attacker, int finalDamage, double elapsedSeconds)
        {
            if (attacker.Digimon.PassiveId != PassiveType.SedeDeBatalha)
                return;

            float multiplier = BattleCombatant.GetHealEffectivenessMultiplier(elapsedSeconds);
            int healAmount = Math.Max(1, (int)(finalDamage * SedeDeBatalhaHealPercentage * multiplier));

            attacker.Digimon.CurrentHealthPoints = Math.Min(
                attacker.Digimon.MaxHealthPoints,
                attacker.Digimon.CurrentHealthPoints + healAmount
            );
        }

        private static int CalculateBaseDamage(BattleCombatant attacker, BattleCombatant defender)
        {
            bool isPhysical = attacker.Digimon.BaseData.AttackType == AttackType.Physical;

            int attack = attacker.GetEffectiveStat(
                isPhysical ? StatType.PhysicalDamage : StatType.SpecialDamage
            );

            int defense = defender.GetEffectiveStat(
                isPhysical ? StatType.PhysicalDefense : StatType.SpecialDefense
            );

            int baseDamage = Math.Max(1, attack - defense);

            if (attacker.Digimon.PassiveId != PassiveType.RompeGuarda)
                return baseDamage;

            int piercedDefense = (int)(defense * (1f - RompeGuardaDefensePierce));
            int piercedDamage = Math.Max(1, attack - piercedDefense);
            int maxGain = (int)(baseDamage * RompeGuardaMaxGainRatio);

            return Math.Min(piercedDamage, baseDamage + maxGain);
        }

        /// <summary>
        /// Aplica passivas de quem está ATACANDO (ver PASSIVAS_SPEC.md hook
        /// on_damage_final_dealt) - sempre depois de tipo/variância/crítico, sempre
        /// multiplicando o dano final (nunca ATK/DEF, ver R1 da spec).
        /// </summary>
        private static int ApplyAttackerPassives(int damage, BattleCombatant attacker, BattleCombatant defender)
        {
            if (attacker.Digimon.PassiveId == PassiveType.FuriaCrescente)
            {
                if (attacker.FuriaCrescenteDecayRemainingSeconds <= 0)
                    attacker.FuriaCrescenteStacks = 0;

                attacker.FuriaCrescenteStacks = Math.Min(FuriaCrescenteMaxStacks, attacker.FuriaCrescenteStacks + 1);
                attacker.FuriaCrescenteDecayRemainingSeconds = FuriaCrescenteDecayWindow;

                damage = (int)(damage * (1f + attacker.FuriaCrescenteStacks * FuriaCrescenteStackBonus));
            }

            if (attacker.Digimon.PassiveId == PassiveType.GolpePesado)
            {
                attacker.GolpePesadoHitCounter++;

                if (attacker.GolpePesadoHitCounter >= GolpePesadoHitInterval)
                {
                    attacker.GolpePesadoHitCounter = 0;
                    damage = (int)(damage * (1f + GolpePesadoBonusDamage));
                }
            }

            if (attacker.Digimon.PassiveId == PassiveType.Execucao && defender.HpPercentage < ExecucaoHpThreshold)
            {
                damage = (int)(damage * (1f + ExecucaoBonusDamage));
            }

            if (attacker.Digimon.PassiveId == PassiveType.GolpeSorrateiro &&
                !attacker.GolpeSorrateiroHitTargets.Contains(defender))
            {
                attacker.GolpeSorrateiroHitTargets.Add(defender);
                damage = (int)(damage * (1f + GolpeSorrateiroBonusDamage));
            }

            if (attacker.Digimon.PassiveId == PassiveType.AlvoMarcado)
            {
                attacker.AlvoMarcadoHitCounter++;

                if (attacker.AlvoMarcadoHitCounter >= AlvoMarcadoHitInterval)
                {
                    attacker.AlvoMarcadoHitCounter = 0;
                    defender.AddEffect(StatType.Speed, AlvoMarcadoSpeedDebuff, AlvoMarcadoDebuffDuration);
                }
            }

            if (attacker.Digimon.PassiveId == PassiveType.Investida && attacker.InvestidaPendingBonus)
            {
                attacker.InvestidaPendingBonus = false;
                damage = (int)(damage * (1f + InvestidaDashBonusDamage));
            }

            if (attacker.Digimon.PassiveId == PassiveType.PosturaFirme)
            {
                damage = (int)(damage * (1f + PosturaFirmeBonusDamage));
            }

            // Fixação (Ranged): só incrementa o contador aqui (todo golpe confirmado é
            // "consecutivo" até uma esquiva zerar - ver BattleUnit.PerformAction). O teto de
            // pilhas e a taxa de redução por pilha vivem em BattleUnit.ComputeEffectiveCooldown,
            // que é quem realmente usa esse contador.
            if (attacker.Digimon.PassiveId == PassiveType.Fixacao)
            {
                attacker.FixacaoConsecutiveHits++;
            }

            return Math.Max(1, damage);
        }

        /// <summary>
        /// Bônus de dano final por conjunto completo da Enciclopédia (ver GameManager.
        /// GetTotalSetDamageBonus/DigimonSetData) - é um bônus do CENTER, não da espécie ou
        /// do time inimigo, então só entra quando quem atacou está no time do jogador
        /// (GameManager.PlayerBattleTeam), nunca pro lado inimigo.
        /// </summary>
        private static int ApplySetBonuses(int damage, BattleCombatant attacker)
        {
            if (!GameManager.Instance.PlayerBattleTeam.Contains(attacker.Digimon))
                return damage;

            float bonus = GameManager.Instance.GetTotalSetBonus(SetEffectType.FinalDamagePercent);

            if (bonus <= 0f)
                return damage;

            return Math.Max(1, (int)(damage * (1f + bonus)));
        }

        /// <summary>Bônus de chance de crítico por conjunto completo (ver SetEffectType.
        /// CritChancePercent) - mesma regra dos outros bônus de conjunto, só o time do
        /// jogador recebe.</summary>
        private static int GetSetCritChanceBonus(BattleCombatant attacker)
        {
            if (!GameManager.Instance.PlayerBattleTeam.Contains(attacker.Digimon))
                return 0;

            return (int)(GameManager.Instance.GetTotalSetBonus(SetEffectType.CritChancePercent) * 100);
        }

        /// <summary>
        /// Aplica passivas de quem está DEFENDENDO (ver PASSIVAS_SPEC.md hook
        /// on_damage_final_taken) - sempre no fim do pipeline, depois das passivas do
        /// atacante. Espinhos precisa do attacker pra saber a Role (só reflete contra melee)
        /// e pra aplicar o reflexo nele.
        /// </summary>
        private static int ApplyDefenderPassives(int damage, BattleCombatant attacker, BattleCombatant defender)
        {
            if (defender.Digimon.PassiveId == PassiveType.UltimaTrincheira &&
                !defender.UltimaTrincheiraTriggered &&
                defender.HpPercentage < UltimaTrincheiraHpThreshold)
            {
                defender.UltimaTrincheiraTriggered = true;
                defender.UltimaTrincheiraRemainingSeconds = UltimaTrincheiraDuration;
            }

            if (defender.UltimaTrincheiraRemainingSeconds > 0)
                damage = (int)(damage * (1f - UltimaTrincheiraDamageReduction));

            if (defender.Digimon.PassiveId == PassiveType.Teimosia)
            {
                float reduction = TeimosiaMaxReduction * (1f - defender.HpPercentage);
                damage = (int)(damage * (1f - reduction));
            }

            if (defender.Digimon.PassiveId == PassiveType.Couraca)
                damage = (int)(damage * (1f - CouracaDamageReduction));

            if (defender.Digimon.PassiveId == PassiveType.GritoDeGuerra && attacker.TauntedBy == defender)
                damage = (int)(damage * (1f - GritoDeGuerraDamageReduction));

            damage = Math.Max(1, damage);

            if (defender.Digimon.PassiveId == PassiveType.Espinhos &&
                MeleeRoles.Contains(attacker.Digimon.BaseData.Role))
            {
                int reflected = Math.Max(1, (int)(damage * EspinhosReflectPercentage));
                attacker.Digimon.TakeDamage(reflected);
            }

            return damage;
        }

        /// <summary>Vigília (Healer): consome o escudo do defensor antes do HP, devolvendo só
        /// o que sobrou pra aplicar como dano de verdade. Sem escudo, devolve o dano
        /// inalterado.</summary>
        private static int ConsumeShield(int damage, BattleCombatant defender)
        {
            if (defender.ShieldAmount <= 0)
                return damage;

            int absorbed = Math.Min(defender.ShieldAmount, damage);
            defender.ShieldAmount -= absorbed;

            return damage - absorbed;
        }

        private static int ApplyTypeAdvantage(int damage, BattleCombatant attacker, BattleCombatant defender)
        {
            float multiplier = TypeAdvantageCalculator.GetDamageMultiplier(
                attacker.Digimon.BaseData,
                defender.Digimon.BaseData
            );

            return Math.Max(1, (int)(damage * multiplier));
        }

        private static int ApplyDamageVariance(int damage)
        {
            float variation = _random.Next(-10, 11) / 100f;

            damage += (int)(damage * variation);

            return Math.Max(1, damage);
        }

        private static int ApplyCritical(int damage, BattleCombatant attacker)
        {
            int critChance = attacker.Digimon.PassiveId == PassiveType.PrecisaoLetal
                ? PrecisaoLetalCritChancePercent
                : BaseCritChancePercent;

            critChance += GetSetCritChanceBonus(attacker);

            int roll = _random.Next(1, 101);

            if (roll <= critChance)
            {
                float multiplier = attacker.Digimon.PassiveId == PassiveType.FioDaLamina
                    ? FioDaLaminaCritMultiplier
                    : BaseCritMultiplier;

                return (int)(damage * multiplier);
            }

            return damage;
        }
    }
}
