using Godot;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Enums;
using System;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>
    /// Sistema responsável por gerenciar um combate entre dois <see cref="DigimonInstance"/>.
    /// Contém lógica de execução de turnos, cálculo de dano (com variação e crítico)
    /// e verificação do resultado da batalha.
    /// </summary>
    public class BattleSystem
    {
        private bool _battleEnded = false;

        private DigimonInstance _player;
        private DigimonInstance _enemy;

        /// <summary>Digimon do jogador envolvido na batalha.</summary>
        public DigimonInstance Player => _player;

        /// <summary>Digimon inimigo envolvido na batalha.</summary>
        public DigimonInstance Enemy => _enemy;

        Random _random = new Random();

        /// <summary>Evento disparado quando o estado da batalha muda (por exemplo, HP alterado).</summary>
        public event Action OnStateChanged;

        /// <summary>
        /// Cria uma nova instância do sistema de batalha com os dois participantes.
        /// </summary>
        /// <param name="player">Instância do digimon do jogador.</param>
        /// <param name="enemy">Instância do digimon inimigo.</param>
        public BattleSystem(DigimonInstance player, DigimonInstance enemy)
        {
            _player = player;
            _enemy = enemy;
        }

        /// <summary>
        /// Calcula e aplica o dano físico de um atacante sobre um defensor.
        /// Processo:
        /// 1) dano base = PhysicalDamage do atacante - PhysicalDefense do defensor (mínimo 1)
        /// 2) aplica variação percentual aleatória
        /// 3) checa crítico (chance fixa)
        /// 4) subtrai HP do defensor e dispara evento de mudança de estado
        /// </summary>
        /// <returns>Quantidade de dano aplicada.</returns>
        private int Attack(DigimonInstance attacker, DigimonInstance defender)
        {
            int damage = CalculateBaseDamage(attacker, defender);

            if (damage < 1)
                damage = 1;

            damage = ApplyDamageVariance(damage);
            damage = ApplyCritical(damage);

            defender.CurrentHealthPoints -= damage;

            if (defender.CurrentHealthPoints < 0)
                defender.CurrentHealthPoints = 0;

            GD.Print($"{attacker.BaseData.Name} causou {damage} de dano em {defender.BaseData.Name}");

            OnStateChanged?.Invoke();

            return damage;
        }

        private int CalculateBaseDamage(DigimonInstance attacker, DigimonInstance defender)
        {
            int attack;
            int defense;

            if (attacker.BaseData.AttackType == AttackType.Physical)
            {
                attack = attacker.CurrentStats.PhysicalDamage;
                defense = defender.CurrentStats.PhysicalDefense;
            }
            else
            {
                attack = attacker.CurrentStats.SpecialDamage;
                defense = defender.CurrentStats.SpecialDefense;
            }

            return Math.Max(1, attack - defense);
        }

        /// <summary>Ativa o ataque do jogador sobre o inimigo.</summary>
        public int PlayerAttack()
        {
            return Attack(_player, _enemy);
        }

        /// <summary>Ativa o ataque do inimigo sobre o jogador.</summary>
        public int EnemyAttack()
        {
            return Attack(_enemy, _player);
        }

        /// <summary>Retorna o estado atual do resultado da batalha (em andamento/vitória/derrota).</summary>
        public BattleResult CheckResult()
        {
            return CheckBattleEnd();
        }

        /// <summary>
        /// Verifica se algum dos participantes ficou com HP <= 0 e define o resultado.
        /// Também marca a batalha como encerrada e dispara o evento de mudança de estado.
        /// </summary>
        private BattleResult CheckBattleEnd()
        {
            if (_enemy.CurrentHealthPoints <= 0)
            {
                _battleEnded = true;
                OnStateChanged?.Invoke();
                return BattleResult.PlayerWon;
            }

            if (_player.CurrentHealthPoints <= 0)
            {
                _battleEnded = true;
                OnStateChanged?.Invoke();
                return BattleResult.EnemyWon;
            }

            return BattleResult.Ongoing;
        }

        /// <summary>
        /// Aplica uma variação percentual aleatória ao dano passado (pequena variação positiva/negativa).
        /// Garante que o dano resultante seja pelo menos 1.
        /// </summary>
        private int ApplyDamageVariance(int damage)
        {
            float variation = (_random.Next(-10, 11)) / 100f;
            damage += (int)(damage * variation);

            return Math.Max(1, damage);
        }

        /// <summary>
        /// Verifica uma rolagem aleatória para crítico e, se crítico, dobra o dano.
        /// Retorna o dano possivelmente modificado.
        /// </summary>
        private int ApplyCritical(int damage)
        {
            int roll = _random.Next(1, 101);

            if(roll <= 10)
            {
                GD.Print("CRITICO!");
                return damage * 2;
            }

            return damage;
        }

        public bool PlayerHasTurn()
        {
            return _player.CurrentStats.Speed >= _enemy.CurrentStats.Speed;
        }

        public BattleResult CheckBattleStatus()
        {
            return CheckBattleEnd();
        }

        /// <summary>
        /// Executa um turno completo respeitando a ordem por velocidade.
        /// - Se a batalha já terminou, retorna estado em andamento.
        /// - Quem tem maior velocidade ataca primeiro; após cada ataque verifica se a batalha terminou.
        /// - Se o inimigo morrer durante o turno do jogador, o jogador recebe experiência.
        /// - Dispara evento de estado ao final do turno.
        /// </summary>
        public BattleResult ExecuteTurn()
        {
            if (_battleEnded)
                return BattleResult.Ongoing;

            BattleResult result;

            if (_player.CurrentStats.Speed >= _enemy.CurrentStats.Speed)
            {
                Attack(_player, _enemy);

                result = CheckBattleEnd();
                if (result != BattleResult.Ongoing)
                {
                    if (result == BattleResult.PlayerWon)
                        OnPlayerVictory();

                    return result;
                }

                Attack(_enemy, _player);

                result = CheckBattleEnd();
                if (result != BattleResult.Ongoing)
                {
                    if (result == BattleResult.PlayerWon)
                        OnPlayerVictory();

                    return result;
                }
            }
            else
            {
                Attack(_enemy, _player);

                result = CheckBattleEnd();
                if (result != BattleResult.Ongoing)
                {
                    if (result == BattleResult.PlayerWon)
                        OnPlayerVictory();

                    return result;
                }

                Attack(_player, _enemy);

                result = CheckBattleEnd();
                if (result != BattleResult.Ongoing)
                {
                    if (result == BattleResult.PlayerWon)
                        OnPlayerVictory();

                    return result;
                }
            }

            OnStateChanged?.Invoke();
            return BattleResult.Ongoing;
        }

        private void OnPlayerVictory()
        {
            GD.Print("PLAYER VENCEU!");
        }

        public void StopBattle()
        {
            _battleEnded = true;
        }
    }
}
