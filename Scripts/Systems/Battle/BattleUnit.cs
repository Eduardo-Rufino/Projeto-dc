using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.UI;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>
    /// Unidade visual de uma batalha 3x3 em tempo real: move, escolhe alvo e ataca sozinha,
    /// segundo a RoleType do Digimon que carrega. Reaproveita o padrão de movimento de
    /// DigimonWorld (Center), mas sempre em relação a um alvo de combate.
    /// </summary>
    public partial class BattleUnit : Node2D
    {
        private const float MeleeRange = 40f;
        private const float AssassinRange = 36f;
        private const float RangedRange = 240f;
        private const float SupportRange = 150f;

        private const float TankSpeed = 80f;
        private const float WarriorSpeed = 80f;
        private const float AssassinSpeed = 105f;
        private const float RangedSpeed = 80f;
        private const float SupportSpeed = 80f;

        // Distância que o alvo pode ter andado do ponto onde o projétil foi mirado
        // pra ser considerado uma esquiva (sem dano aplicado).
        private const float ProjectileDodgeDistance = 40f;

        private const double TankCooldown = 1.6;
        private const double WarriorCooldown = 1.4;
        private const double AssassinCooldown = 1.2;
        private const double RangedCooldown = 1.8;
        private const double HealCooldown = 2.5;
        private const double BuffDebuffCooldown = 3.0;

        private const float HealPercentage = 0.15f;
        private const float BuffDebuffPercentage = 0.2f;
        private const double BuffDebuffDuration = 6.0;

        // A Speed real do combate: o cooldown de ação escala pela razão entre a Speed do
        // atacante e a do alvo (2x mais rápido = ataca ~2x mais vezes no mesmo intervalo),
        // mas nunca fica mais rápido que esse piso - evita golpes múltiplos no mesmo segundo
        // quando a diferença de Speed é enorme.
        private const double MinAttackCooldown = 0.5;

        // Influência leve do stat de Speed na velocidade de movimento (a Role continua
        // mandando na velocidade base) - clamp de ±15% em cima do valor da Role.
        private const float SpeedStatReference = 20f;
        private const float SpeedStatMultiplierMin = 0.85f;
        private const float SpeedStatMultiplierMax = 1.15f;

        private const float KitingMinRangeFactor = 0.5f;

        // Só troca o lado que o sprite olha se o alvo estiver a mais que isso de distância
        // horizontal - evita ficar virando de lado à toa quando o alvo está quase alinhado.
        private const float FacingDeadZone = 10f;

        // Enquanto está dentro do alcance esperando o cooldown, a unidade circula em volta
        // do alvo em vez de ficar parada trocando golpes - deixa o combate mais vivo.
        private const float OrbitRadiusFactor = 0.7f;
        private const float MinOrbitRadius = 55f;
        private const float OrbitSpeedFactor = 0.5f;
        private const float OrbitAngularSpeedMin = 1.0f;
        private const float OrbitAngularSpeedMax = 2.2f;

        // Ranged se afasta de QUALQUER inimigo que chegue mais perto que isso, não só do
        // seu alvo de ataque - é o que faz ele se reposicionar pra fugir de quem está
        // avançando, em vez de só manter distância do alvo escolhido.
        private const float RangedDangerRadius = 130f;

        // Assassin/Ranged escolhem alvo por uma pontuação (distância + uma penalidade por
        // Role), não por uma regra rígida - assim um Tank bem perto/no alcance ainda pode
        // ser atacado (não é ignorado), mas um DPS a uma distância parecida é preferido.
        private const float TargetPriorityPenaltyStep = 70f;
        private const double RetargetInterval = 3.0;

        // Taunt do Tank: de tempos em tempos, força quem está perto (só quem causa dano -
        // Support não é afetado, já que ele não "ataca" ninguém) a mirar nele, abrindo
        // espaço pro DPS/suporte aliado fugir de quem estava perseguindo eles.
        private const double TauntInterval = 8.0;
        private const double TauntDuration = 4.0;
        private const float TauntRadius = 220f;

        private double _tauntTimer;
        private BattleCombatant _forcedTarget;
        private double _forcedTargetRemaining;

        private double _retargetTimer;

        private static readonly Color DamageColor = new(0.95f, 0.25f, 0.2f);
        private static readonly Color HealColor = new(0.35f, 0.9f, 0.4f);
        private static readonly Color BuffColor = new(0.35f, 0.65f, 1f);
        private static readonly Color DebuffColor = new(0.85f, 0.4f, 0.9f);

        private float _orbitAngle;
        private float _orbitAngularSpeed;

        private DigimonSprite _sprite;
        private Control _hpBarRoot;
        private ColorRect _hpBarFill;
        private float _hpBarMaxWidth;

        private BattleArena _arena;
        private BattleCombatant _combatant;
        private bool _isPlayerSide;

        private BattleCombatant _target;
        private double _attackCooldownRemaining;
        private bool _isActing;
        private bool _isDead;

        private float _speed;
        private float _range;
        private double _actionCooldown;

        public BattleCombatant Combatant => _combatant;
        public bool IsPlayerSide => _isPlayerSide;
        public bool IsDead => _isDead;

        public override void _Ready()
        {
            _sprite = GetNode<DigimonSprite>("DigimonSprite");
            _hpBarRoot = GetNode<Control>("HpBar");
            _hpBarFill = GetNode<ColorRect>("HpBar/Fill");
            _hpBarMaxWidth = _hpBarFill.Size.X;
        }

        public void Initialize(BattleCombatant combatant, bool isPlayerSide, BattleArena arena)
        {
            _combatant = combatant;
            _isPlayerSide = isPlayerSide;
            _arena = arena;

            _sprite.SetDigimon(combatant.Digimon.BaseData.Code);
            _sprite.SetDirection(!isPlayerSide);

            _orbitAngle = (float)GD.RandRange(0.0, Mathf.Tau);

            float angularSpeed = (float)GD.RandRange(OrbitAngularSpeedMin, OrbitAngularSpeedMax);
            _orbitAngularSpeed = GD.Randf() < 0.5f ? angularSpeed : -angularSpeed;

            // Sorteia o primeiro taunt pra não sincronizar Tanks dos dois lados.
            _tauntTimer = GD.RandRange(TauntInterval * 0.5, TauntInterval);

            ConfigureByRole();

            UpdateHpBar();
        }

        private void ConfigureByRole()
        {
            var digimon = _combatant.Digimon;

            switch (digimon.BaseData.Role)
            {
                case RoleType.Tank:
                    _speed = TankSpeed;
                    _range = MeleeRange;
                    _actionCooldown = TankCooldown;
                    break;

                case RoleType.Warrior:
                    _speed = WarriorSpeed;
                    _range = MeleeRange;
                    _actionCooldown = WarriorCooldown;
                    break;

                case RoleType.Assassin:
                    _speed = AssassinSpeed;
                    _range = AssassinRange;
                    _actionCooldown = AssassinCooldown;
                    break;

                case RoleType.Ranged:
                    _speed = RangedSpeed;
                    _range = RangedRange;
                    _actionCooldown = RangedCooldown;
                    break;

                case RoleType.Support:
                    _speed = SupportSpeed;
                    _range = SupportRange;
                    _actionCooldown = digimon.BaseData.SupportType == SupportType.Healer
                        ? HealCooldown
                        : BuffDebuffCooldown;
                    break;
            }

            // A Role decide a velocidade base; o stat de Speed do próprio Digimon só ajusta
            // um pouco pra cima ou pra baixo em cima disso (efeito leve, a Role continua
            // sendo o fator principal do quão rápido a unidade se move).
            _speed *= GetSpeedStatMultiplier();
        }

        private float GetSpeedStatMultiplier()
        {
            float speedStat = _combatant.GetEffectiveStat(StatType.Speed);

            return Mathf.Clamp(speedStat / SpeedStatReference, SpeedStatMultiplierMin, SpeedStatMultiplierMax);
        }

        /// <summary>
        /// Cooldown de ação real, considerando a Speed do atacante em relação à do alvo
        /// atual: o dobro de Speed ataca ~2x mais rápido, a metade ataca na metade da
        /// frequência - mas nunca mais rápido que MinAttackCooldown, pra não deixar golpes
        /// múltiplos no mesmo segundo quando a diferença é extrema.
        /// </summary>
        private double ComputeEffectiveCooldown()
        {
            if (_target == null)
                return _actionCooldown;

            int attackerSpeed = Math.Max(1, _combatant.GetEffectiveStat(StatType.Speed));
            int targetSpeed = Math.Max(1, _target.GetEffectiveStat(StatType.Speed));

            double speedRatio = (double)attackerSpeed / targetSpeed;

            double effectiveCooldown = _actionCooldown / speedRatio;

            return Math.Max(effectiveCooldown, MinAttackCooldown);
        }

        public override void _Process(double delta)
        {
            if (_isDead || _combatant == null)
                return;

            if (_combatant.IsDead)
            {
                Die();
                return;
            }

            if (_arena.IsBattleOver)
            {
                _sprite.SetWalking(false);
                return;
            }

            UpdateHpBar();

            if (_combatant.Digimon.BaseData.Role == RoleType.Tank)
            {
                _tauntTimer -= delta;

                if (_tauntTimer <= 0)
                {
                    _tauntTimer = TauntInterval;

                    PerformTaunt();
                }
            }

            if (_isActing)
                return;

            _attackCooldownRemaining -= delta;
            _retargetTimer -= delta;

            if (_forcedTargetRemaining > 0)
            {
                _forcedTargetRemaining -= delta;

                if (_forcedTarget != null && !_forcedTarget.IsDead)
                {
                    _target = _forcedTarget;
                }
                else
                {
                    _forcedTargetRemaining = 0;
                }
            }

            if (_forcedTargetRemaining <= 0)
            {
                bool dueForRetarget = _retargetTimer <= 0 && UsesDynamicTargeting;

                if (!IsTargetValid(_target) || dueForRetarget)
                {
                    if (dueForRetarget)
                        _retargetTimer = RetargetInterval;

                    _target = SelectTarget();
                }
            }

            if (_target == null)
            {
                _sprite.SetWalking(false);
                return;
            }

            BattleUnit targetUnit = _arena.GetUnitFor(_target);

            if (targetUnit == null)
                return;

            float distance = GlobalPosition.DistanceTo(targetUnit.GlobalPosition);

            MoveTowardTarget(targetUnit.GlobalPosition, distance, delta);

            if (distance <= _range && _attackCooldownRemaining <= 0)
            {
                _ = PerformAction();
            }
        }

        /// <summary>
        /// Só quem sempre mira um inimigo (nunca um aliado) foge quando o alvo chega perto
        /// demais. Healer e Buffer miram aliados - fugir do próprio aliado não faz sentido,
        /// então eles só se aproximam até o alcance e param. Ranged tem sua própria lógica
        /// de posicionamento (ComputeRangedPosition), que reage a qualquer ameaça próxima,
        /// não só ao alvo de ataque.
        /// </summary>
        private bool UsesKiting =>
            _combatant.Digimon.BaseData.Role == RoleType.Support &&
            _combatant.Digimon.BaseData.SupportType == SupportType.Debuffer;

        private void MoveTowardTarget(Vector2 targetPosition, float distance, double delta)
        {
            Vector2 previousPosition = GlobalPosition;
            Vector2 desiredPosition;

            if (_combatant.Digimon.BaseData.Role == RoleType.Ranged)
            {
                desiredPosition = ComputeRangedPosition(targetPosition, distance, delta);
            }
            else if (UsesKiting)
            {
                float minDistance = _range * KitingMinRangeFactor;

                if (distance < minDistance)
                {
                    Vector2 away = (GlobalPosition - targetPosition).Normalized();
                    desiredPosition = GlobalPosition + away * (float)(_speed * delta);
                }
                else if (distance > _range)
                {
                    desiredPosition = GlobalPosition.MoveToward(targetPosition, (float)(_speed * delta));
                }
                else
                {
                    desiredPosition = ComputeOrbitPosition(targetPosition, delta);
                }
            }
            else if (distance > _range)
            {
                desiredPosition = GlobalPosition.MoveToward(targetPosition, (float)(_speed * delta));
            }
            else
            {
                desiredPosition = ComputeOrbitPosition(targetPosition, delta);
            }

            // Não deixa a unidade sair da área delimitada pelos hexágonos da arena. Se ela
            // já estiver fora por algum motivo (posição inicial imprecisa, etc.), não trava -
            // só bloqueia movimentos que SAIRIAM de dentro pra fora.
            if (_arena.IsPointInsideArena(previousPosition) &&
                !_arena.IsPointInsideArena(desiredPosition))
            {
                desiredPosition = previousPosition;
            }

            // Olha sempre pro alvo, não pra direção instantânea do passo - orbitar/circular
            // faz o X do movimento trocar de sinal a cada frame perto do topo/base do
            // círculo, e seguir isso literalmente deixava o sprite "piscando" de lado.
            float facingDelta = targetPosition.X - GlobalPosition.X;

            if (Mathf.Abs(facingDelta) > FacingDeadZone)
            {
                _sprite.SetDirection(facingDelta > 0);
            }

            if (desiredPosition == previousPosition)
            {
                _sprite.SetWalking(false);
                return;
            }

            GlobalPosition = desiredPosition;

            _sprite.SetWalking(true);
        }

        /// <summary>
        /// Ponto de destino pra quando a unidade já está no alcance e só esperando o
        /// cooldown: circula em volta do alvo (a um raio um pouco menor que o alcance,
        /// com um mínimo pra não virar um shuffle minúsculo no corpo a corpo) em vez de
        /// ficar parada no lugar entre um ataque e outro.
        /// </summary>
        private Vector2 ComputeOrbitPosition(Vector2 targetPosition, double delta)
        {
            _orbitAngle += _orbitAngularSpeed * (float)delta;

            float orbitRadius = Mathf.Max(_range * OrbitRadiusFactor, MinOrbitRadius);

            Vector2 offset = new Vector2(
                Mathf.Cos(_orbitAngle),
                Mathf.Sin(_orbitAngle)
            ) * orbitRadius;

            Vector2 orbitPoint = targetPosition + offset;

            return GlobalPosition.MoveToward(
                orbitPoint,
                (float)(_speed * OrbitSpeedFactor * delta)
            );
        }

        /// <summary>
        /// Posicionamento do Ranged: prioriza se afastar de qualquer inimigo que esteja
        /// perto demais (não só do alvo de ataque escolhido), e só quando não há ameaça
        /// próxima é que ele se preocupa em chegar no alcance do alvo. Isso faz ele buscar
        /// ativamente uma posição segura, em vez de só manter distância de quem está
        /// atacando.
        /// </summary>
        private Vector2 ComputeRangedPosition(Vector2 targetPosition, float distanceToTarget, double delta)
        {
            BattleUnit nearestThreat = FindNearestEnemyUnit();

            if (nearestThreat != null)
            {
                float threatDistance = GlobalPosition.DistanceTo(nearestThreat.GlobalPosition);

                if (threatDistance < RangedDangerRadius)
                {
                    Vector2 away = (GlobalPosition - nearestThreat.GlobalPosition).Normalized();

                    return GlobalPosition + away * (float)(_speed * delta);
                }
            }

            float minDistance = _range * KitingMinRangeFactor;

            if (distanceToTarget < minDistance)
            {
                Vector2 away = (GlobalPosition - targetPosition).Normalized();

                return GlobalPosition + away * (float)(_speed * delta);
            }

            if (distanceToTarget > _range)
            {
                return GlobalPosition.MoveToward(targetPosition, (float)(_speed * delta));
            }

            return ComputeOrbitPosition(targetPosition, delta);
        }

        /// <summary>Inimigo vivo mais próximo, independente de quem é o alvo de ataque atual -
        /// usado pelo Ranged pra saber de quem fugir.</summary>
        private BattleUnit FindNearestEnemyUnit()
        {
            BattleTeam enemyTeam = _isPlayerSide ? _arena.Match.EnemyTeam : _arena.Match.PlayerTeam;

            return enemyTeam.AliveMembers
                .Select(m => _arena.GetUnitFor(m))
                .Where(u => u != null)
                .OrderBy(u => GlobalPosition.DistanceTo(u.GlobalPosition))
                .FirstOrDefault();
        }

        private bool IsTargetValid(BattleCombatant target)
        {
            if (target == null || target.IsDead)
                return false;

            var digimon = _combatant.Digimon;

            if (digimon.BaseData.Role == RoleType.Support &&
                digimon.BaseData.SupportType == SupportType.Healer)
            {
                return target.HpPercentage < 1f;
            }

            return true;
        }

        /// <summary>
        /// Provocação do Tank: força os inimigos próximos que causam dano (não afeta
        /// Support, que não ataca) a mirar nele por um tempo, dando espaço pro DPS/suporte
        /// aliado escapar de quem estava perseguindo eles.
        /// </summary>
        private void PerformTaunt()
        {
            BattleTeam enemyTeam = _isPlayerSide ? _arena.Match.EnemyTeam : _arena.Match.PlayerTeam;

            foreach (var enemyCombatant in enemyTeam.AliveMembers)
            {
                if (enemyCombatant.Digimon.BaseData.Role == RoleType.Support)
                    continue;

                BattleUnit enemyUnit = _arena.GetUnitFor(enemyCombatant);

                if (enemyUnit == null)
                    continue;

                if (GlobalPosition.DistanceTo(enemyUnit.GlobalPosition) <= TauntRadius)
                {
                    enemyUnit.ApplyTaunt(_combatant, TauntDuration);
                }
            }

            GD.Print($"{_combatant.Digimon.BaseData.Name} provocou os inimigos próximos!");
        }

        /// <summary>Força essa unidade a mirar em <paramref name="taunter"/> por
        /// <paramref name="duration"/> segundos, ignorando a seleção normal de alvo.</summary>
        public void ApplyTaunt(BattleCombatant taunter, double duration)
        {
            _forcedTarget = taunter;
            _forcedTargetRemaining = duration;
            _target = taunter;
        }

        private BattleCombatant SelectTarget()
        {
            var digimon = _combatant.Digimon;

            BattleTeam allyTeam = _isPlayerSide ? _arena.Match.PlayerTeam : _arena.Match.EnemyTeam;
            BattleTeam enemyTeam = _isPlayerSide ? _arena.Match.EnemyTeam : _arena.Match.PlayerTeam;

            var aliveEnemies = enemyTeam.AliveMembers;

            if (digimon.BaseData.Role == RoleType.Support)
            {
                switch (digimon.BaseData.SupportType)
                {
                    case SupportType.Healer:
                        // Sem ninguém ferido, o Healer fica parado (não ataca -
                        // ApplyAction() sempre cura o alvo escolhido, nunca causa dano).
                        return allyTeam.AliveMembers
                            .Where(m => m.HpPercentage < 1f)
                            .OrderBy(m => m.HpPercentage)
                            .FirstOrDefault();

                    case SupportType.Buffer:
                        return allyTeam.AliveMembers
                            .OrderByDescending(m => m.GetEffectiveStat(m.AttackStat))
                            .FirstOrDefault();

                    case SupportType.Debuffer:
                        return aliveEnemies
                            .OrderByDescending(m => m.GetEffectiveStat(m.AttackStat))
                            .FirstOrDefault();
                }
            }

            if (digimon.BaseData.Role == RoleType.Tank)
            {
                // Tenta interceptar quem está ameaçando o próprio suporte/ranged (o time
                // "atrás" dele), em vez de simplesmente ir pro inimigo mais próximo de si
                // mesmo - isso dá função real ao Tank: ele se intromete no caminho de quem
                // está indo pra cima do DPS aliado.
                var backline = allyTeam.AliveMembers
                    .Where(m => m.Digimon.BaseData.Role == RoleType.Support ||
                                m.Digimon.BaseData.Role == RoleType.Ranged)
                    .Select(m => _arena.GetUnitFor(m))
                    .Where(u => u != null)
                    .ToList();

                if (backline.Count > 0)
                {
                    return aliveEnemies
                        .Select(m => (combatant: m, unit: _arena.GetUnitFor(m)))
                        .Where(x => x.unit != null)
                        .OrderBy(x => backline.Min(b => x.unit.GlobalPosition.DistanceTo(b.GlobalPosition)))
                        .Select(x => x.combatant)
                        .FirstOrDefault();
                }

                return NearestOf(aliveEnemies);
            }

            if (digimon.BaseData.Role == RoleType.Assassin || digimon.BaseData.Role == RoleType.Ranged)
            {
                // Pontuação em vez de regra rígida: um Tank bem perto/no alcance ainda pode
                // ser atacado (não é ignorado), mas um DPS a uma distância parecida ganha.
                return aliveEnemies
                    .Select(m => (combatant: m, unit: _arena.GetUnitFor(m)))
                    .Where(x => x.unit != null)
                    .OrderBy(x => GetTargetScore(x.combatant, x.unit.GlobalPosition))
                    .Select(x => x.combatant)
                    .FirstOrDefault();
            }

            // Warrior: engaja quem estiver mais perto, segurando a linha de frente.
            return NearestOf(aliveEnemies);
        }

        /// <summary>Roles cujo alvo ideal muda com a posição de todo mundo no campo (Tank
        /// guardando o backline, Assassin/Ranged pontuando por distância+Role) - reavaliam
        /// o alvo periodicamente, não só quando o atual morre.</summary>
        private bool UsesDynamicTargeting =>
            _combatant.Digimon.BaseData.Role == RoleType.Tank ||
            _combatant.Digimon.BaseData.Role == RoleType.Assassin ||
            _combatant.Digimon.BaseData.Role == RoleType.Ranged;

        /// <summary>
        /// Pontuação de alvo pra Assassin/Ranged: distância real + uma penalidade por Role
        /// (menor pontuação = mais atraente). O Tank recebe a maior penalidade, então só é
        /// escolhido quando está bem mais perto que as alternativas - não é ignorado, mas
        /// também não trava a unidade nele quando há um DPS alcançável.
        /// </summary>
        private float GetTargetScore(BattleCombatant candidate, Vector2 candidateUnitPosition)
        {
            float distance = GlobalPosition.DistanceTo(candidateUnitPosition);
            float penalty = GetTargetPriority(candidate.Digimon.BaseData.Role) * TargetPriorityPenaltyStep;

            return distance + penalty;
        }

        /// <summary>Menor valor = mais prioritário como alvo pro Assassin/Ranged.</summary>
        private static int GetTargetPriority(RoleType role) => role switch
        {
            RoleType.Support => 0,
            RoleType.Ranged => 1,
            RoleType.Warrior => 2,
            RoleType.Assassin => 3,
            RoleType.Tank => 4,
            _ => 5
        };

        private BattleCombatant NearestOf(System.Collections.Generic.List<BattleCombatant> candidates)
        {
            return candidates
                .Select(c => (combatant: c, unit: _arena.GetUnitFor(c)))
                .Where(x => x.unit != null)
                .OrderBy(x => GlobalPosition.DistanceTo(x.unit.GlobalPosition))
                .Select(x => x.combatant)
                .FirstOrDefault();
        }

        private async Task PerformAction()
        {
            _isActing = true;
            _attackCooldownRemaining = ComputeEffectiveCooldown();

            _sprite.SetWalking(false);

            await _sprite.PlayAttack();

            // A unidade pode ter morrido enquanto a animação tocava (o AnimatedSprite2D é
            // compartilhado, e Die() troca a animação pra "SickLose" - se isso disparar um
            // AnimationFinished, esse await acorda de novo e reafirmaria a pose de Idle por
            // cima da de morte). Reafirma a pose de morte e para tudo aqui.
            if (_isDead)
            {
                _sprite.SetWalking(false);
                _sprite.PlayDeath();
                return;
            }

            if (_combatant.Digimon.BaseData.Role == RoleType.Ranged &&
                _target != null && !_target.IsDead)
            {
                BattleUnit targetUnit = _arena.GetUnitFor(_target);

                if (targetUnit != null)
                {
                    Vector2 aimedPosition = targetUnit.GlobalPosition;

                    await FireProjectile(aimedPosition);

                    if (_isDead)
                    {
                        _sprite.SetWalking(false);
                        _sprite.PlayDeath();
                        return;
                    }

                    // Se o alvo já morreu (projétil cancelado no meio do voo) ou andou pra
                    // longe de onde foi mirado, não aplica dano - ApplyAction() já barra o
                    // alvo morto sozinho, então só o caso de esquiva precisa de um aviso.
                    if (_target.IsDead)
                    {
                        _isActing = false;
                        return;
                    }

                    if (targetUnit.GlobalPosition.DistanceTo(aimedPosition) > ProjectileDodgeDistance)
                    {
                        GD.Print($"{_target.Digimon.BaseData.Name} desviou do ataque à distância!");

                        _isActing = false;
                        return;
                    }
                }
            }

            ApplyAction();

            _isActing = false;
        }

        /// <summary>
        /// Lança o sprite de ataque à distância (escolhido pelo Element do atacante) do
        /// atacante até a posição do alvo. Se o alvo morrer no meio do voo (outro atacante
        /// mais rápido o derrubou primeiro), o projétil some ali mesmo em vez de continuar
        /// e "acertar" visualmente um Digimon que já está caído.
        /// </summary>
        private async Task FireProjectile(Vector2 targetPosition)
        {
            var scene = GD.Load<PackedScene>("res://Scenes/Battle/AttackProjectile.tscn");
            var projectile = scene.Instantiate<AttackProjectile>();

            _arena.AddToWorld(projectile);

            var texture = AttackVisuals.GetProjectileTexture(_combatant.Digimon.BaseData.Element);

            bool arrived = false;

            projectile.Arrived += () => arrived = true;

            projectile.Launch(GlobalPosition, targetPosition, texture);

            while (!arrived)
            {
                if (_target != null && _target.IsDead)
                {
                    if (GodotObject.IsInstanceValid(projectile))
                        projectile.QueueFree();

                    return;
                }

                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }

        private void ApplyAction()
        {
            if (_isDead || _target == null || _target.IsDead)
                return;

            var digimon = _combatant.Digimon;

            BattleUnit targetUnit = _arena.GetUnitFor(_target);

            if (targetUnit == null)
                return;

            if (digimon.BaseData.Role == RoleType.Support)
            {
                switch (digimon.BaseData.SupportType)
                {
                    case SupportType.Healer:
                        int healed = ApplyHeal();
                        targetUnit.ShowFloatingText($"+{healed}", HealColor);
                        return;

                    case SupportType.Buffer:
                        _target.AddEffect(_target.AttackStat, BuffDebuffPercentage, BuffDebuffDuration);
                        targetUnit.ShowFloatingText($"+{GetStatAbbreviation(_target.AttackStat)}", BuffColor);
                        return;

                    case SupportType.Debuffer:
                        _target.AddEffect(_target.AttackStat, -BuffDebuffPercentage, BuffDebuffDuration);
                        targetUnit.ShowFloatingText($"-{GetStatAbbreviation(_target.AttackStat)}", DebuffColor);
                        return;
                }
            }

            int damage = _arena.Match.ResolveAttack(_combatant, _target);

            targetUnit.ShowFloatingText($"-{damage}", DamageColor);

            if (_target.IsDead)
            {
                targetUnit.Die();
            }
            else
            {
                _ = targetUnit.PlayHitReaction();
            }
        }

        /// <summary>Mostra um texto flutuante (dano, cura, buff/debuff) acima dessa unidade.</summary>
        public void ShowFloatingText(string text, Color color)
        {
            var scene = GD.Load<PackedScene>("res://Scenes/Battle/DamageIndicator.tscn");
            var indicator = scene.Instantiate<DamageIndicator>();

            _arena.AddToWorld(indicator);

            indicator.Initialize(GlobalPosition + new Vector2(0, -20f), text, color);
        }

        /// <summary>Abreviação do stat pro indicador de buff/debuff - já cobre Speed/Defense
        /// pra quando existirem buffs desses tipos, mesmo só ATK sendo usado hoje.</summary>
        private static string GetStatAbbreviation(StatType stat) => stat switch
        {
            StatType.PhysicalDamage => "ATK",
            StatType.SpecialDamage => "ATK",
            StatType.PhysicalDefense => "DEF",
            StatType.SpecialDefense => "DEF",
            StatType.Speed => "VEL",
            _ => "?"
        };

        /// <summary>Cura o alvo e retorna a quantidade de HP realmente recuperada (pode ser
        /// menor que o valor nominal se o Digimon já estava quase cheio).</summary>
        private int ApplyHeal()
        {
            var targetDigimon = _target.Digimon;

            int healAmount = (int)(targetDigimon.MaxHealthPoints * HealPercentage);
            int before = targetDigimon.CurrentHealthPoints;

            targetDigimon.CurrentHealthPoints = Math.Min(
                targetDigimon.MaxHealthPoints,
                targetDigimon.CurrentHealthPoints + healAmount
            );

            return targetDigimon.CurrentHealthPoints - before;
        }

        public async Task PlayHitReaction()
        {
            await _sprite.PlayHit();

            // Mesma proteção de PerformAction(): se morreu durante a animação de dano,
            // reafirma a pose de morte por cima de qualquer Idle que o await tenha soltado.
            if (_isDead)
            {
                _sprite.SetWalking(false);
                _sprite.PlayDeath();
            }
        }

        public void Die()
        {
            if (_isDead)
                return;

            _isDead = true;

            _sprite.SetWalking(false);
            _sprite.PlayDeath();

            if (_hpBarRoot != null)
                _hpBarRoot.Visible = false;
        }

        private void UpdateHpBar()
        {
            var digimon = _combatant.Digimon;

            float pct = digimon.MaxHealthPoints <= 0
                ? 0f
                : (float)digimon.CurrentHealthPoints / digimon.MaxHealthPoints;

            _hpBarFill.Size = new Vector2(
                _hpBarMaxWidth * Mathf.Clamp(pct, 0f, 1f),
                _hpBarFill.Size.Y
            );
        }
    }
}
