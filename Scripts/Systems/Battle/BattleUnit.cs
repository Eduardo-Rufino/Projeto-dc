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

        // Precisa ficar acima da velocidade de Tank/Warrior/Support (todos em 80): a fuga de
        // ameaça do Ranged (ComputeRangedPosition) depende de conseguir abrir distância de
        // quem está perseguindo - com a mesma velocidade, os dois ficam empatados pra sempre
        // e o Ranged nunca mais volta a atacar (é o que segurava a fuga a vida toda).
        private const float RangedSpeed = 92f;

        private const float SupportSpeed = 80f;

        // Distância que o alvo pode ter andado do ponto onde o projétil foi mirado
        // pra ser considerado uma esquiva (sem dano aplicado).
        private const float ProjectileDodgeDistance = 40f;
        private const float PredicaoDodgeDistanceMultiplier = 0.5f;

        private const double TankCooldown = 1.6;
        private const double WarriorCooldown = 1.4;
        private const double AssassinCooldown = 1.2;
        private const double RangedCooldown = 1.8;
        private const double HealCooldown = 2.5;
        private const double BuffDebuffCooldown = 3.0;

        private const float HealPercentage = 0.15f;
        private const float MaosFirmesHealPercentage = 0.19f;
        private const float VigiliaShieldPercentage = 0.08f;
        private const float BuffDebuffPercentage = 0.2f;
        private const double BuffDebuffDuration = 6.0;

        // Aura Vital (Support, ver PASSIVAS_SPEC.md): regen contínua num raio - a spec não
        // dá um raio nem um percentual explícitos ("regeneração passiva num raio"), então
        // reaproveita SupportRange (150, o próprio alcance de engajamento do Support) como
        // raio e a mesma taxa de Fôlego (0.6%/s) como percentual - default razoável, não um
        // número da spec sendo ajustado.
        private const float AuraVitalRegenPercentPerSecond = 0.006f;

        // A Speed real do combate: o cooldown de ação escala pela razão entre a Speed do
        // atacante e a do alvo (2x mais rápido = ataca ~2x mais vezes no mesmo intervalo),
        // mas nunca fica mais rápido que esse piso - evita golpes múltiplos no mesmo segundo
        // quando a diferença de Speed é enorme.
        private const double MinAttackCooldown = 0.5;

        // Fixação (Ranged) - ver ComputeEffectiveCooldown. 4 pilhas x 0.06s = -0.24s, o teto
        // que a spec descreve.
        private const double FixacaoCooldownReductionPerHit = 0.06;
        private const int FixacaoMaxCooldownReductionStacks = 4;

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

        // Teto de segurança: MinOrbitRadius (55) é maior que o alcance de Warrior/Tank (40)
        // e Assassin (36) - sem esse teto, orbitar empurrava esses papéis pra FORA do
        // próprio alcance de ataque, criando um vaivém (entra no alcance, orbita pra fora,
        // se aproxima de novo, orbita pra fora de novo...) que raramente deixava o cooldown
        // zerar com o alvo realmente dentro do alcance - na prática, quase não batiam.
        private const float MaxOrbitRangeFactor = 0.85f;

        private const float OrbitSpeedFactor = 0.5f;
        private const float OrbitAngularSpeedMin = 1.0f;
        private const float OrbitAngularSpeedMax = 2.2f;

        // Ranged se afasta de QUALQUER inimigo que chegue mais perto que isso, não só do
        // seu alvo de ataque - é o que faz ele se reposicionar pra fugir de quem está
        // avançando, em vez de só manter distância do alvo escolhido.
        private const float RangedDangerRadius = 130f;
        private const float PesLevesRangedDangerRadius = 190f;
        private const float PesLevesFleeSpeedMultiplier = 1.08f;

        // Assassin/Ranged escolhem alvo por uma pontuação (distância + uma penalidade por
        // Role), não por uma regra rígida - assim um Tank bem perto/no alcance ainda pode
        // ser atacado (não é ignorado), mas um DPS a uma distância parecida é preferido.
        private const float TargetPriorityPenaltyStep = 70f;
        private const float InfiltradorTargetPriorityPenaltyStep = 130f;
        private const double RetargetInterval = 3.0;

        // Dash do melee (Warrior/Tank): quando o alvo está fora do alcance corpo a corpo mas
        // já perto o suficiente, dá um impulso curto de velocidade extra em vez de andar no
        // passo normal - é o que dá ao melee uma chance real de fechar a distância contra um
        // Ranged fugindo (que sozinho é mais rápido que o melee - ver RangedSpeed). O mesmo
        // impulso também ajuda a desviar de ataques à distância: o deslocamento repentino
        // tende a passar do ProjectileDodgeDistance calculado em FireProjectile/ApplyAction,
        // então não precisa de nenhuma lógica de esquiva separada.
        private const float MeleeDashRange = 180f;
        private const float MeleeDashSpeedMultiplier = 5f;
        private const double MeleeDashDuration = 0.2;
        private const double MeleeDashCooldown = 6.0;

        // Investida (Warrior) e Passo Fantasma (Assassin) encurtam o cooldown do próprio
        // dash - ver PASSIVAS_SPEC.md.
        private const double FasterDashCooldown = 4.0;

        private double _dashCooldownRemaining;
        private double _dashTimeRemaining;

        // Segurança contra perseguição impossível: Warrior/Tank/Support não reavaliam alvo
        // periodicamente (só quando o atual morre) - se o alvo escolhido for mais rápido e
        // nunca deixar a distância chegar no alcance (ex.: um Ranged fugindo de outra ameaça
        // enquanto foge desse perseguidor também), a unidade fica perseguindo pra sempre sem
        // nunca atacar, tirando ela do combate pro resto da luta. Se passar tempo demais sem
        // sequer CHEGAR no alcance (não é sobre esperar cooldown), força uma troca de alvo.
        private const double StuckChaseTimeout = 6.0;

        // Taunt do Tank: de tempos em tempos, força quem está perto (só quem causa dano -
        // Support não é afetado, já que ele não "ataca" ninguém) a mirar nele, abrindo
        // espaço pro DPS/suporte aliado fugir de quem estava perseguindo eles.
        private const double TauntInterval = 8.0;
        private const double ProvocacaoInsistenteTauntInterval = 6.5;
        private const double TauntDuration = 4.0;
        private const float TauntRadius = 220f;

        private double _tauntTimer;
        private BattleCombatant _forcedTarget;
        private double _forcedTargetRemaining;

        private double _retargetTimer;
        private double _stuckChaseTimer;

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
        private SelectionEllipse _selectionEllipse;
        private SelectionEllipse _selectedRing;
        private bool _isSelected;

        // Mesmas cores pra barra de vida e pro anel no chão - verde time do jogador,
        // vermelho time inimigo, pra identificar o lado de cada unidade de relance.
        private static readonly Color PlayerSideColor = new(0.2f, 0.85f, 0.25f, 1f);
        private static readonly Color EnemySideColor = new(0.9f, 0.25f, 0.2f, 1f);

        private BattleArena _arena;
        private BattleCombatant _combatant;
        private bool _isPlayerSide;

        private BattleCombatant _target;
        private double _attackCooldownRemaining;
        private bool _isActing;
        private bool _isDead;

        /// <summary>True quando um Suporte (Healer/Buffer/Debuffer) não tem mais nenhuma
        /// ação de suporte útil pra fazer no alvo escolhido por SelectTarget (curar/dar
        /// escudo sem ninguém ferido/sem escudo, ou buffar/debuffar um alvo que já está no
        /// teto de pilhas) - nesse caso ele ataca normalmente em vez de ficar reaplicando um
        /// efeito sem valor pra sempre. Sem isso, dois Suportes puros (ex.: Buffer vs Buffer)
        /// nunca causam dano um no outro e a luta 1x1 não tem como terminar. Setado por
        /// SelectTarget, consumido uma vez por ApplyAction logo em seguida.</summary>
        private bool _supportFallbackAttack;

        private float _speed;
        private float _range;
        private double _actionCooldown;

        public BattleCombatant Combatant => _combatant;
        public bool IsPlayerSide => _isPlayerSide;
        public bool IsDead => _isDead;
        public float Speed => _speed;

        public override void _Ready()
        {
            _sprite = GetNode<DigimonSprite>("DigimonSprite");
            _hpBarRoot = GetNode<Control>("HpBar");
            _hpBarFill = GetNode<ColorRect>("HpBar/Fill");
            _hpBarMaxWidth = _hpBarFill.Size.X;
            _selectionEllipse = GetNode<SelectionEllipse>("SelectionEllipse");
            _selectedRing = GetNode<SelectionEllipse>("SelectedRing");
        }

        public void Initialize(BattleCombatant combatant, bool isPlayerSide, BattleArena arena)
        {
            _combatant = combatant;
            _isPlayerSide = isPlayerSide;
            _arena = arena;

            Color sideColor = isPlayerSide ? PlayerSideColor : EnemySideColor;

            _hpBarFill.Color = sideColor;
            _selectionEllipse.SetColor(sideColor);

            _sprite.SetDigimon(combatant.Digimon.BaseData.Code);
            _sprite.SetDirection(!isPlayerSide);

            _orbitAngle = (float)GD.RandRange(0.0, Mathf.Tau);

            float angularSpeed = (float)GD.RandRange(OrbitAngularSpeedMin, OrbitAngularSpeedMax);
            _orbitAngularSpeed = GD.Randf() < 0.5f ? angularSpeed : -angularSpeed;

            // Sorteia o primeiro taunt pra não sincronizar Tanks dos dois lados.
            double tauntInterval = GetTauntInterval();
            _tauntTimer = GD.RandRange(tauntInterval * 0.5, tauntInterval);

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

            // Fixação (Ranged, ver PASSIVAS_SPEC.md): cada acerto consecutivo (sem esquiva no
            // meio - ver PerformAction) desconta um valor FIXO de segundos, até um teto de
            // pilhas - desconto aplicado depois da escala por Speed, não em cima dela, porque
            // a spec fala em segundos, não em %.
            if (_combatant.Digimon.PassiveId == PassiveType.Fixacao)
            {
                int stacks = Math.Min(FixacaoMaxCooldownReductionStacks, _combatant.FixacaoConsecutiveHits);
                effectiveCooldown -= FixacaoCooldownReductionPerHit * stacks;
            }

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
                    _tauntTimer = GetTauntInterval();

                    PerformTaunt();
                }
            }

            // Aura Vital (Support, ver PASSIVAS_SPEC.md) - regen contínua num raio ao redor
            // dessa unidade, independente de estar atacando/se movendo/em cooldown (por isso
            // roda aqui, antes do "if (_isActing) return" abaixo).
            if (_combatant.Digimon.PassiveId == PassiveType.AuraVital)
                ApplyAuraVitalRegen(delta);

            if (_isActing)
                return;

            _attackCooldownRemaining -= delta;
            _retargetTimer -= delta;
            _dashCooldownRemaining -= delta;

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
                _combatant.TauntedBy = null;

            if (_forcedTargetRemaining <= 0)
            {
                bool dueForRetarget = (_retargetTimer <= 0 && UsesDynamicTargeting) ||
                    _stuckChaseTimer >= StuckChaseTimeout;

                if (!IsTargetValid(_target) || dueForRetarget)
                {
                    if (dueForRetarget)
                    {
                        _retargetTimer = RetargetInterval;
                        _stuckChaseTimer = 0;
                    }

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

            // Só conta como "perseguição travada" o tempo em que nunca chegou no alcance -
            // esperar o cooldown já dentro do alcance não é o problema que isso previne.
            _stuckChaseTimer = distance <= _range ? 0 : _stuckChaseTimer + delta;

            if (IsMeleeRole &&
                _dashTimeRemaining <= 0 &&
                _dashCooldownRemaining <= 0 &&
                distance > _range &&
                distance <= MeleeDashRange)
            {
                _dashTimeRemaining = MeleeDashDuration;
                _dashCooldownRemaining = GetDashCooldown();

                // Investida (Warrior, ver PASSIVAS_SPEC.md) - o bônus de dano em si vive em
                // DamageCalculator.ApplyAttackerPassives, que consome essa flag no próximo
                // golpe acertado.
                if (_combatant.Digimon.PassiveId == PassiveType.Investida)
                    _combatant.InvestidaPendingBonus = true;
            }

            MoveTowardTarget(targetUnit.GlobalPosition, distance, delta);

            if (distance <= _range && _attackCooldownRemaining <= 0)
            {
                _ = PerformAction();
            }
        }

        /// <summary>
        /// Só quem sempre mira um inimigo (nunca um aliado) foge quando o alvo chega perto
        /// demais. Healer e Buffer miram aliados - fugir do próprio aliado não faz sentido,
        /// então eles só se aproximam até o alcance e param (a menos que tenham Instinto de
        /// Fuga - ver PASSIVAS_SPEC.md - que estende esse kiting pra eles também; a fuga
        /// respeitar o alcance do aliado já vem de graça, o bloco abaixo usa _range/_target
        /// igual pra qualquer Role). Ranged tem sua própria lógica de posicionamento
        /// (ComputeRangedPosition), que reage a qualquer ameaça próxima, não só ao alvo de
        /// ataque.
        /// </summary>
        private bool UsesKiting =>
            _combatant.Digimon.BaseData.Role == RoleType.Support &&
            (_combatant.Digimon.BaseData.SupportType == SupportType.Debuffer ||
             _combatant.Digimon.PassiveId == PassiveType.InstintoDeFuga);

        /// <summary>Papéis que usam MeleeRange (alcance 40) - só eles recebem o dash, já que
        /// Assassin (range menor, mas o mais rápido do jogo) não sofre do mesmo problema de
        /// nunca alcançar um Ranged fugindo. Passo Fantasma (ver PASSIVAS_SPEC.md) é a
        /// exceção deliberada: concede o dash a um Assassin específico mesmo sem esse
        /// problema, só pra fechar o gap inicial mais rápido.</summary>
        private bool IsMeleeRole =>
            _combatant.Digimon.BaseData.Role == RoleType.Warrior ||
            _combatant.Digimon.BaseData.Role == RoleType.Tank ||
            _combatant.Digimon.PassiveId == PassiveType.PassoFantasma;

        private void MoveTowardTarget(Vector2 targetPosition, float distance, double delta)
        {
            Vector2 previousPosition = GlobalPosition;
            Vector2 desiredPosition;

            if (_dashTimeRemaining > 0)
            {
                _dashTimeRemaining -= delta;

                desiredPosition = GlobalPosition.MoveToward(
                    targetPosition,
                    (float)(_speed * MeleeDashSpeedMultiplier * delta)
                );
            }
            else if (_combatant.Digimon.BaseData.Role == RoleType.Ranged)
            {
                desiredPosition = _combatant.Digimon.PassiveId == PassiveType.PosturaFirme
                    ? ComputeStandGroundPosition(targetPosition, distance, delta)
                    : ComputeRangedPosition(targetPosition, distance, delta);
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
                // Perto da borda, o círculo de órbita (ou o passo de fuga do kiting/Ranged)
                // pode cair inteiro fora da arena - travando a unidade parada ali pra sempre,
                // ainda atacando normalmente (o alcance do ataque não depende do movimento ter
                // dado certo). Em vez de só desistir do movimento, tenta andar direto pro alvo,
                // que normalmente puxa de volta pra dentro.
                Vector2 towardTarget = GlobalPosition.MoveToward(targetPosition, (float)(_speed * delta));

                if (_arena.IsPointInsideArena(towardTarget))
                {
                    desiredPosition = towardTarget;
                }
                else
                {
                    // Nem o alvo ajuda (ex.: o próprio alvo está do outro lado de um canto
                    // apertado entre dois hexágonos) - isso era o caso que ainda travava a
                    // unidade de vez, porque o fallback antigo simplesmente desistia do
                    // movimento (desiredPosition = previousPosition) e o frame seguinte caía
                    // exatamente na mesma decisão, preso pra sempre. Anda em direção ao centro
                    // da arena em vez disso - um ponto sempre válido por construção -, e se
                    // nem esse passo (curto) bastar pra sair da borda, salta direto pro centro,
                    // que nunca falha o teste de dentro/fora.
                    Vector2 towardCenter = GlobalPosition.MoveToward(_arena.ArenaCenter, (float)(_speed * delta));

                    desiredPosition = _arena.IsPointInsideArena(towardCenter)
                        ? towardCenter
                        : _arena.ArenaCenter;
                }
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
        /// com um mínimo pra não virar um shuffle minúsculo no corpo a corpo, mas nunca
        /// passando do próprio alcance de ataque) em vez de ficar parada no lugar entre
        /// um ataque e outro.
        /// </summary>
        private Vector2 ComputeOrbitPosition(Vector2 targetPosition, double delta)
        {
            _orbitAngle += _orbitAngularSpeed * (float)delta;

            float orbitRadius = Mathf.Min(
                Mathf.Max(_range * OrbitRadiusFactor, MinOrbitRadius),
                _range * MaxOrbitRangeFactor
            );

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

        /// <summary>Postura Firme (Ranged, ver PASSIVAS_SPEC.md): desliga órbita E fuga por
        /// completo - só se aproxima até o alcance e para ali, sem se importar com nenhuma
        /// ameaça próxima. O dano final maior compensa a exposição.</summary>
        private Vector2 ComputeStandGroundPosition(Vector2 targetPosition, float distance, double delta)
        {
            if (distance > _range)
                return GlobalPosition.MoveToward(targetPosition, (float)(_speed * delta));

            return GlobalPosition;
        }

        /// <summary>
        /// Posicionamento do Ranged: enquanto ainda não chegou no alcance do próprio alvo,
        /// prioriza se afastar de qualquer inimigo que esteja perto demais (não só do alvo
        /// escolhido), buscando ativamente uma posição segura em vez de só manter distância
        /// de quem está atacando. Mas se já está no alcance do alvo (já pode atacar), não
        /// abandona mais essa posição só por causa de uma ameaça genérica passando perto -
        /// senão um perseguidor da mesma velocidade prende o Ranged num impasse de fuga
        /// permanente, sem nunca mais voltar a atacar. Nesse caso o único gatilho de fuga
        /// que resta é ficar perto demais do próprio alvo (minDistance, abaixo).
        /// </summary>
        /// <summary>Pés Leves (Ranged) amplia o raio de fuga - ver PASSIVAS_SPEC.md.</summary>
        private float GetRangedDangerRadius() =>
            _combatant.Digimon.PassiveId == PassiveType.PesLeves
                ? PesLevesRangedDangerRadius
                : RangedDangerRadius;

        /// <summary>Pés Leves (Ranged) acelera especificamente o movimento de FUGA (R4 da
        /// spec: velocidade_movimento, não stat_Speed) - só usado nos passos "away" abaixo,
        /// nunca na aproximação/órbita normal.</summary>
        private float GetFleeSpeed() =>
            _combatant.Digimon.PassiveId == PassiveType.PesLeves
                ? _speed * PesLevesFleeSpeedMultiplier
                : _speed;

        private Vector2 ComputeRangedPosition(Vector2 targetPosition, float distanceToTarget, double delta)
        {
            bool alreadyInRangeOfTarget = distanceToTarget <= _range;

            if (!alreadyInRangeOfTarget)
            {
                BattleUnit nearestThreat = FindNearestEnemyUnit();

                if (nearestThreat != null)
                {
                    float threatDistance = GlobalPosition.DistanceTo(nearestThreat.GlobalPosition);

                    if (threatDistance < GetRangedDangerRadius())
                    {
                        Vector2 away = (GlobalPosition - nearestThreat.GlobalPosition).Normalized();

                        return GlobalPosition + away * (float)(GetFleeSpeed() * delta);
                    }
                }
            }

            float minDistance = _range * KitingMinRangeFactor;

            if (distanceToTarget < minDistance)
            {
                Vector2 away = (GlobalPosition - targetPosition).Normalized();

                return GlobalPosition + away * (float)(GetFleeSpeed() * delta);
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

            // Enquanto não estiver no fallback de ataque (ver _supportFallbackAttack), um
            // Suporte só continua "travado" no alvo atual enquanto a ação de suporte nele
            // ainda fizer sentido - assim que deixar de fazer (curou até encher, ou o buff/
            // debuff bateu no teto de pilhas), força uma nova SelectTarget em vez de ficar
            // reaplicando a mesma ação sem efeito novo pra sempre.
            if (digimon.BaseData.Role == RoleType.Support && !_supportFallbackAttack)
                return IsSupportActionStillUseful(target);

            return true;
        }

        /// <summary>Se a ação de suporte (curar/dar escudo/buffar/debuffar) em
        /// <paramref name="target"/> ainda teria algum efeito novo - usado tanto por
        /// SelectTarget (decidir se cai no fallback de ataque) quanto por IsTargetValid
        /// (forçar reavaliação assim que deixar de valer a pena). Sem essa checagem
        /// compartilhada, um Suporte travava no primeiro alvo escolhido pro resto da luta
        /// inteira (Support não usa UsesDynamicTargeting como Tank/Assassin/Ranged).
        ///
        /// Buffer/Debuffer usam a COMPOSIÇÃO do time como critério (tem alguém que causa
        /// dano direto?), não o estado momentâneo das pilhas de buff/debuff - pilhas decaem
        /// sozinhas com o tempo (ver AddEffect/BuffDebuffDuration, mais longa que o
        /// cooldown, mas não o bastante pra nunca cair de novo abaixo do teto), então usar
        /// "já está no teto agora" como sinal fazia o Suporte alternar pra sempre entre
        /// atacar e voltar a buffar toda vez que uma pilha antiga expirava - exatamente o
        /// impasse relatado de Suporte vs Suporte no 1x1, que nunca se resolvia porque o
        /// ataque nunca virava um compromisso.</summary>
        private bool IsSupportActionStillUseful(BattleCombatant target)
        {
            var digimon = _combatant.Digimon;

            switch (digimon.BaseData.SupportType)
            {
                case SupportType.Healer:
                    if (target.HpPercentage < 1f)
                        return true;

                    // Vigília (ver PASSIVAS_SPEC.md): sem ninguém ferido, o alvo escolhido é
                    // quem não tem escudo - continua útil enquanto isso for verdade.
                    if (digimon.PassiveId == PassiveType.Vigilia)
                        return target.ShieldAmount <= 0;

                    return false;

                case SupportType.Buffer:
                case SupportType.Debuffer:
                    // Buffar aliado ou debuffar inimigo só compensa se o PRÓPRIO time tiver
                    // alguém pra converter isso em dano de verdade - senão nunca compensa,
                    // não importa quantas pilhas o alvo já tem.
                    BattleTeam allyTeam = _isPlayerSide ? _arena.Match.PlayerTeam : _arena.Match.EnemyTeam;

                    return allyTeam.AliveMembers.Any(m => m.Digimon.BaseData.Role != RoleType.Support);

                default:
                    return true;
            }
        }

        /// <summary>
        /// Provocação do Tank: força os inimigos próximos que causam dano (não afeta
        /// Support, que não ataca) a mirar nele por um tempo, dando espaço pro DPS/suporte
        /// aliado escapar de quem estava perseguindo eles.
        /// </summary>
        /// <summary>Provocação Insistente (Tank) reduz o intervalo entre Taunts - ver
        /// PASSIVAS_SPEC.md.</summary>
        private double GetTauntInterval() =>
            _combatant.Digimon.PassiveId == PassiveType.ProvocacaoInsistente
                ? ProvocacaoInsistenteTauntInterval
                : TauntInterval;

        /// <summary>Investida (Warrior) e Passo Fantasma (Assassin) encurtam o próprio
        /// cooldown de dash - ver PASSIVAS_SPEC.md.</summary>
        private double GetDashCooldown() =>
            _combatant.Digimon.PassiveId == PassiveType.Investida ||
            _combatant.Digimon.PassiveId == PassiveType.PassoFantasma
                ? FasterDashCooldown
                : MeleeDashCooldown;

        /// <summary>Aura Vital (Support, ver PASSIVAS_SPEC.md): regenera todo aliado vivo
        /// dentro de SupportRange dessa unidade (incluindo ela mesma, trivialmente dentro do
        /// próprio raio) - ver o comentário em AuraVitalRegenPercentPerSecond sobre a
        /// ausência de raio/percentual explícitos na spec.</summary>
        private void ApplyAuraVitalRegen(double delta)
        {
            BattleTeam allyTeam = _isPlayerSide ? _arena.Match.PlayerTeam : _arena.Match.EnemyTeam;

            foreach (var ally in allyTeam.AliveMembers)
            {
                BattleUnit allyUnit = _arena.GetUnitFor(ally);

                if (allyUnit == null)
                    continue;

                if (GlobalPosition.DistanceTo(allyUnit.GlobalPosition) > SupportRange)
                    continue;

                ally.ApplyContinuousRegen(AuraVitalRegenPercentPerSecond, delta, _arena.Match.ElapsedSeconds);
            }
        }

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
        }

        /// <summary>Força essa unidade a mirar em <paramref name="taunter"/> por
        /// <paramref name="duration"/> segundos, ignorando a seleção normal de alvo.</summary>
        public void ApplyTaunt(BattleCombatant taunter, double duration)
        {
            _forcedTarget = taunter;
            _forcedTargetRemaining = duration;
            _target = taunter;

            // Grito de Guerra (Tank, ver PASSIVAS_SPEC.md) lê isso via DamageCalculator, que
            // só enxerga BattleCombatant - _forcedTarget/_forcedTargetRemaining (aqui em
            // cima) não bastam porque são privados desse BattleUnit.
            _combatant.TauntedBy = taunter;
        }

        private BattleCombatant SelectTarget()
        {
            var digimon = _combatant.Digimon;

            BattleTeam allyTeam = _isPlayerSide ? _arena.Match.PlayerTeam : _arena.Match.EnemyTeam;
            BattleTeam enemyTeam = _isPlayerSide ? _arena.Match.EnemyTeam : _arena.Match.PlayerTeam;

            var aliveEnemies = enemyTeam.AliveMembers;

            if (digimon.BaseData.Role == RoleType.Support)
            {
                _supportFallbackAttack = false;

                switch (digimon.BaseData.SupportType)
                {
                    case SupportType.Healer:
                        var hurtAlly = allyTeam.AliveMembers
                            .Where(m => m.HpPercentage < 1f)
                            .OrderBy(m => m.HpPercentage)
                            .FirstOrDefault();

                        if (hurtAlly != null)
                            return hurtAlly;

                        // Vigília (ver PASSIVAS_SPEC.md): sem ninguém ferido, em vez de
                        // atacar, dá escudo pro primeiro aliado ainda sem um (ver
                        // ApplyVigiliaShield/IsSupportActionStillUseful).
                        if (digimon.PassiveId == PassiveType.Vigilia)
                        {
                            var shieldless = allyTeam.AliveMembers
                                .Where(m => m.ShieldAmount <= 0)
                                .FirstOrDefault();

                            if (shieldless != null)
                                return shieldless;
                        }

                        // Nada pra curar nem dar escudo - sem isso o Healer ficaria parado
                        // pra sempre (contra outro Suporte puro, a luta nunca terminaria).
                        // Ataca normalmente em vez disso.
                        _supportFallbackAttack = true;

                        return NearestOf(aliveEnemies);

                    case SupportType.Buffer:
                        {
                            var buffTarget = allyTeam.AliveMembers
                                .OrderByDescending(m => m.GetEffectiveStat(m.AttackStat))
                                .FirstOrDefault();

                            if (buffTarget == null)
                                return null;

                            if (IsSupportActionStillUseful(buffTarget))
                                return buffTarget;

                            // O próprio time não tem ninguém pra converter esse buff em dano
                            // (ver IsSupportActionStillUseful) - sem isso, um Buffer contra
                            // outro Suporte puro ficaria buffando pra sempre e a luta 1x1
                            // nunca terminaria.
                            _supportFallbackAttack = true;

                            return NearestOf(aliveEnemies);
                        }

                    case SupportType.Debuffer:
                        {
                            var debuffTarget = aliveEnemies
                                .OrderByDescending(m => m.GetEffectiveStat(m.AttackStat))
                                .FirstOrDefault();

                            if (debuffTarget == null)
                                return null;

                            // O alvo já é um inimigo (Debuffer nunca mira aliado) - no
                            // fallback, ataca esse mesmo alvo em vez de escolher outro.
                            _supportFallbackAttack = !IsSupportActionStillUseful(debuffTarget);

                            return debuffTarget;
                        }
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
        /// o alvo periodicamente, não só quando o atual morre. Fixação (Ranged, ver
        /// PASSIVAS_SPEC.md) é a exceção: trava no alvo escolhido na primeira seleção (que já
        /// prioriza o Support/Ranged inimigo, ver GetTargetScore) até ele morrer - o teto de
        /// segurança StuckChaseTimeout (mais abaixo, fora desse método) continua valendo
        /// mesmo assim, não é "reavaliar" no sentido estratégico, é só evitar perseguição
        /// impossível pra sempre. Support também entra aqui pelo mesmo motivo de fundo: sem
        /// reavaliação periódica, IsTargetValid sozinho não bastava pra perceber a tempo que
        /// um buff/debuff bateu no teto de pilhas (ver IsSupportActionStillUseful) e cair no
        /// fallback de ataque - é assim que o impasse de Suporte vs Suporte era resolvido.</summary>
        private bool UsesDynamicTargeting =>
            _combatant.Digimon.PassiveId != PassiveType.Fixacao &&
            (_combatant.Digimon.BaseData.Role == RoleType.Tank ||
             _combatant.Digimon.BaseData.Role == RoleType.Assassin ||
             _combatant.Digimon.BaseData.Role == RoleType.Ranged ||
             _combatant.Digimon.BaseData.Role == RoleType.Support);

        /// <summary>
        /// Pontuação de alvo pra Assassin/Ranged: distância real + uma penalidade por Role
        /// (menor pontuação = mais atraente). O Tank recebe a maior penalidade, então só é
        /// escolhido quando está bem mais perto que as alternativas - não é ignorado, mas
        /// também não trava a unidade nele quando há um DPS alcançável.
        /// </summary>
        private float GetTargetScore(BattleCombatant candidate, Vector2 candidateUnitPosition)
        {
            float distance = GlobalPosition.DistanceTo(candidateUnitPosition);

            float penaltyStep = _combatant.Digimon.PassiveId == PassiveType.Infiltrador
                ? InfiltradorTargetPriorityPenaltyStep
                : TargetPriorityPenaltyStep;

            float penalty = GetTargetPriority(candidate.Digimon.BaseData.Role) * penaltyStep;

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
            // Confere de novo bem na hora de começar: o alvo pode ter morrido entre o
            // instante em que o _Process decidiu chamar PerformAction() e agora (outro
            // atacante resolveu o dele primeiro nesse mesmo quadro).
            if (_target != null && _target.IsDead)
                return;

            _isActing = true;
            _attackCooldownRemaining = ComputeEffectiveCooldown();

            _sprite.SetWalking(false);

            bool cancelledMidSwing = await PlayAttackWatchingTarget();

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

            // O alvo morreu no meio do próprio golpe (outro atacante mais rápido o derrubou
            // primeiro) - a animação já foi cortada pra Idle em PlayAttackWatchingTarget(),
            // então não "conecta" visualmente o soco/mordida num Digimon já caído.
            if (cancelledMidSwing)
            {
                _isActing = false;
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

                    // A margem de esquiva precisa escalar com quanto tempo o projétil ficou
                    // no ar (tiros de longo alcance voam bem mais tempo) e com a velocidade
                    // real do alvo - um limite fixo (ex.: 40) fazia até movimento normal
                    // (orbitando, perseguindo outra coisa) em tiros de longa distância passar
                    // fácil da marca, contando como "esquiva" um golpe que claramente acertou.
                    float flightDistance = GlobalPosition.DistanceTo(aimedPosition);
                    float flightSeconds = flightDistance / AttackProjectile.TravelSpeed;

                    float dodgeDistance = _combatant.Digimon.PassiveId == PassiveType.Predicao
                        ? ProjectileDodgeDistance * PredicaoDodgeDistanceMultiplier
                        : ProjectileDodgeDistance;

                    float dodgeThreshold = dodgeDistance + targetUnit.Speed * flightSeconds;

                    if (targetUnit.GlobalPosition.DistanceTo(aimedPosition) > dodgeThreshold)
                    {
                        // Fixação (Ranged, ver PASSIVAS_SPEC.md): esquiva quebra a sequência
                        // de acertos consecutivos que reduz o cooldown.
                        if (_combatant.Digimon.PassiveId == PassiveType.Fixacao)
                            _combatant.FixacaoConsecutiveHits = 0;

                        _isActing = false;
                        return;
                    }
                }
            }

            ApplyAction();

            _isActing = false;
        }

        /// <summary>
        /// Toca a animação de ataque quadro a quadro (em vez de só esperar o
        /// AnimationFinished) pra poder cortar pra Idle na hora se o alvo morrer no meio do
        /// golpe - outro atacante mais rápido pode ter derrubado o mesmo alvo primeiro.
        /// Sem isso, o golpe terminava de tocar inteiro e visualmente "acertava" um Digimon
        /// que já tinha caído. Retorna true se foi cortada assim.
        ///
        /// Espera especificamente pela animação "Attack" continuar tocando (não só "alguma
        /// coisa" estar tocando): essa unidade pode tomar um golpe e ter o próprio sprite
        /// tomado de assalto por PlayHit() no meio do seu golpe, e se isso terminar caindo
        /// num Idle (que fica em loop pra sempre), esperar por "IsPlaying()" genérico nunca
        /// mais desligaria - travando essa unidade parada, sem atacar nem se mover, pro
        /// resto da luta (era exatamente esse o bug: unidade morre parada depois de tomar
        /// dois golpes em sequência enquanto ainda estava com o próprio ataque em curso).
        /// </summary>
        private async Task<bool> PlayAttackWatchingTarget()
        {
            _sprite.Play("Attack");

            while (_sprite.IsPlayingAnimation("Attack"))
            {
                if (_isDead)
                    return false;

                if (_target != null && _target.IsDead)
                {
                    _sprite.PlayIdle();
                    return true;
                }

                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            // Só força Idle se "Attack" realmente terminou sozinho - se outra coisa já
            // assumiu o sprite nesse meio tempo (ex.: essa unidade tomou um golpe e entrou
            // em PlayHit()), não pisa em cima disso.
            if (_sprite.CurrentAnimation == "Attack")
                _sprite.PlayIdle();

            return false;
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

            // _supportFallbackAttack (ver SelectTarget) sinaliza que não sobrou nenhuma ação
            // de suporte útil pro alvo escolhido - cai direto pro ataque normal (fora deste
            // if) em vez de curar/buffar/debuffar de novo à toa.
            if (digimon.BaseData.Role == RoleType.Support && !_supportFallbackAttack)
            {
                switch (digimon.BaseData.SupportType)
                {
                    case SupportType.Healer:
                        // _target sem estar ferido só acontece via o fallback de Vigília em
                        // SelectTarget (o caminho normal só escolhe quem está ferido) -
                        // seguro usar isso como sinal de "é escudo, não cura".
                        if (digimon.PassiveId == PassiveType.Vigilia && _target.HpPercentage >= 1f)
                        {
                            ApplyVigiliaShield(targetUnit);
                        }
                        else
                        {
                            int healed = ApplyHeal();
                            targetUnit.ShowFloatingText($"+{healed}", HealColor);
                        }
                        return;

                    case SupportType.Buffer:
                        if (digimon.PassiveId == PassiveType.DuplaVoz)
                        {
                            ApplyDuplaVozBuff();
                        }
                        else if (digimon.PassiveId == PassiveType.Ressonancia)
                        {
                            ApplyRessonanciaBuff(targetUnit);
                        }
                        else
                        {
                            _target.AddEffect(_target.AttackStat, BuffDebuffPercentage, BuffDebuffDuration);
                            targetUnit.ShowFloatingText($"+{GetStatAbbreviation(_target.AttackStat)}", BuffColor);
                        }
                        return;

                    case SupportType.Debuffer:
                        if (digimon.PassiveId == PassiveType.MarcaDupla)
                        {
                            ApplyMarcaDuplaDebuff();
                        }
                        else
                        {
                            _target.AddEffect(_target.AttackStat, -BuffDebuffPercentage, BuffDebuffDuration);
                            targetUnit.ShowFloatingText($"-{GetStatAbbreviation(_target.AttackStat)}", DebuffColor);
                        }
                        return;
                }
            }

            int damage = _arena.Match.ResolveAttack(_combatant, _target);

            // Mesmo cálculo que DamageCalculator já aplicou de verdade no dano acima -
            // só pra mostrar pro jogador que aquele golpe teve vantagem/desvantagem de
            // tipo, sem precisar que ResolveAttack devolva mais que o número final.
            float typeMultiplier = TypeAdvantageCalculator.GetDamageMultiplier(
                _combatant.Digimon.BaseData,
                _target.Digimon.BaseData
            );

            string damageText = typeMultiplier == 1.0f
                ? $"-{damage}"
                : $"-{damage} x{typeMultiplier:0.#}";

            targetUnit.ShowFloatingText(damageText, DamageColor);

            if (_target.IsDead)
            {
                targetUnit.Die();
            }
            else
            {
                _ = targetUnit.PlayHitReaction();
            }

            // Golpe Pesado (Warrior, ver PASSIVAS_SPEC.md) - DamageCalculator.
            // ApplyAttackerPassives já aplicou o bônus de dano nesse golpe e zerou o
            // contador; aqui só falta o splash em área, que precisa de posição/time inimigo
            // (fora do alcance de BattleCombatant/DamageCalculator).
            if (_combatant.Digimon.PassiveId == PassiveType.GolpePesado && _combatant.GolpePesadoHitCounter == 0)
            {
                ApplyGolpePesadoSplash(damage, targetUnit.GlobalPosition);
            }

            // Projétil Perfurante (Ranged, ver PASSIVAS_SPEC.md) - o alvo principal já tomou
            // o dano cheio via ResolveAttack; aqui só falta o segundo inimigo atravessado.
            if (_combatant.Digimon.PassiveId == PassiveType.ProjetilPerfurante)
            {
                ApplyProjetilPerfurante(damage, targetUnit);
            }
        }

        // "Área pequena" não tem um número na spec - 70 escolhido entre o alcance de melee
        // (40) e o raio de Taunt (220): maior que o alcance de um único alvo, sem cobrir a
        // arena inteira.
        private const float GolpePesadoAreaRadius = 70f;

        /// <summary>Golpe Pesado (Warrior): splash do golpe bonificado pros inimigos vivos
        /// perto do alvo principal (que já tomou o dano cheio via ResolveAttack, não é
        /// atingido de novo aqui). Aplica o dano direto (TakeDamage), sem reentrar em
        /// ResolveAttack - um splash não deveria re-rolar variância/crítico/passivas por
        /// conta própria.</summary>
        private void ApplyGolpePesadoSplash(int damage, Vector2 targetPosition)
        {
            BattleTeam enemyTeam = _isPlayerSide ? _arena.Match.EnemyTeam : _arena.Match.PlayerTeam;

            foreach (var enemy in enemyTeam.AliveMembers)
            {
                if (enemy == _target)
                    continue;

                BattleUnit enemyUnit = _arena.GetUnitFor(enemy);

                if (enemyUnit == null)
                    continue;

                if (enemyUnit.GlobalPosition.DistanceTo(targetPosition) > GolpePesadoAreaRadius)
                    continue;

                enemy.Digimon.TakeDamage(damage);
                enemyUnit.ShowFloatingText($"-{damage}", DamageColor);

                if (enemy.IsDead)
                    enemyUnit.Die();
                else
                    _ = enemyUnit.PlayHitReaction();
            }
        }

        private const float ProjetilPerfuranteDamagePercentage = 0.50f;

        /// <summary>Projétil Perfurante (Ranged): atravessa o alvo principal (que já tomou o
        /// dano cheio via ResolveAttack) e atinge o inimigo vivo mais próximo dele por
        /// metade do dano - mesmo padrão do splash de Golpe Pesado (TakeDamage direto, sem
        /// reentrar em ResolveAttack), só que num único segundo alvo em vez de todos numa
        /// área.</summary>
        private void ApplyProjetilPerfurante(int damage, BattleUnit primaryTargetUnit)
        {
            BattleTeam enemyTeam = _isPlayerSide ? _arena.Match.EnemyTeam : _arena.Match.PlayerTeam;

            BattleCombatant secondCombatant = null;
            BattleUnit secondUnit = null;
            float bestDistance = float.MaxValue;

            foreach (var enemy in enemyTeam.AliveMembers)
            {
                if (enemy == _target)
                    continue;

                BattleUnit enemyUnit = _arena.GetUnitFor(enemy);

                if (enemyUnit == null)
                    continue;

                float distance = enemyUnit.GlobalPosition.DistanceTo(primaryTargetUnit.GlobalPosition);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    secondCombatant = enemy;
                    secondUnit = enemyUnit;
                }
            }

            if (secondCombatant == null)
                return;

            int pierceDamage = Math.Max(1, (int)(damage * ProjetilPerfuranteDamagePercentage));

            secondCombatant.Digimon.TakeDamage(pierceDamage);
            secondUnit.ShowFloatingText($"-{pierceDamage}", DamageColor);

            if (secondCombatant.IsDead)
                secondUnit.Die();
            else
                _ = secondUnit.PlayHitReaction();
        }

        /// <summary>Mostra um texto flutuante (dano, cura, buff/debuff) acima dessa unidade.</summary>
        public void ShowFloatingText(string text, Color color)
        {
            var scene = GD.Load<PackedScene>("res://Scenes/Battle/DamageIndicator.tscn");
            var indicator = scene.Instantiate<DamageIndicator>();

            _arena.AddToWorld(indicator);

            indicator.Initialize(GlobalPosition + new Vector2(0, -20f), text, color);
        }

        private const float DuplaVozPercentage = 0.12f;
        private const float MarcaDuplaPercentage = -0.14f;
        private const int RessonanciaMaxStacks = 3;
        private const float RessonanciaThirdStackPercentage = 0.10f;

        /// <summary>Dupla Voz (Buffer, ver PASSIVAS_SPEC.md): buffa os dois maiores
        /// atacantes aliados (não só _target, que já é o primeiro) em vez de um só com o
        /// dobro do percentual.</summary>
        private void ApplyDuplaVozBuff()
        {
            BattleTeam allyTeam = _isPlayerSide ? _arena.Match.PlayerTeam : _arena.Match.EnemyTeam;

            var topTwo = allyTeam.AliveMembers
                .OrderByDescending(m => m.GetEffectiveStat(m.AttackStat))
                .Take(2);

            foreach (var ally in topTwo)
            {
                ally.AddEffect(ally.AttackStat, DuplaVozPercentage, BuffDebuffDuration);

                BattleUnit allyUnit = _arena.GetUnitFor(ally);

                allyUnit?.ShowFloatingText($"+{GetStatAbbreviation(ally.AttackStat)}", BuffColor);
            }
        }

        /// <summary>Marca Dupla (Debuffer, ver PASSIVAS_SPEC.md): espelho de Dupla Voz, nos
        /// dois maiores atacantes inimigos.</summary>
        private void ApplyMarcaDuplaDebuff()
        {
            BattleTeam enemyTeam = _isPlayerSide ? _arena.Match.EnemyTeam : _arena.Match.PlayerTeam;

            var topTwo = enemyTeam.AliveMembers
                .OrderByDescending(m => m.GetEffectiveStat(m.AttackStat))
                .Take(2);

            foreach (var enemy in topTwo)
            {
                enemy.AddEffect(enemy.AttackStat, MarcaDuplaPercentage, BuffDebuffDuration);

                BattleUnit enemyUnit = _arena.GetUnitFor(enemy);

                enemyUnit?.ShowFloatingText($"-{GetStatAbbreviation(enemy.AttackStat)}", DebuffColor);
            }
        }

        /// <summary>Ressonância (Buffer, ver PASSIVAS_SPEC.md): permite uma 3ª pilha no
        /// mesmo alvo, mas ela vale menos que as duas primeiras - por isso precisa saber
        /// quantas pilhas de ATK esse alvo já tem ANTES de chamar AddEffect, pra escolher o
        /// percentual certo.</summary>
        private void ApplyRessonanciaBuff(BattleUnit targetUnit)
        {
            int existingStacks = _target.Effects.Count(e => e.Stat == _target.AttackStat && e.Percentage > 0);

            float percentage = existingStacks >= 2
                ? RessonanciaThirdStackPercentage
                : BuffDebuffPercentage;

            _target.AddEffect(_target.AttackStat, percentage, BuffDebuffDuration, RessonanciaMaxStacks);
            targetUnit.ShowFloatingText($"+{GetStatAbbreviation(_target.AttackStat)}", BuffColor);
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
        /// menor que o valor nominal se o Digimon já estava quase cheio, ou por causa da
        /// eficácia reduzida em lutas muito longas - ver GetHealEffectivenessMultiplier).</summary>
        private int ApplyHeal()
        {
            var targetDigimon = _target.Digimon;

            float healPercentage = _combatant.Digimon.PassiveId == PassiveType.MaosFirmes
                ? MaosFirmesHealPercentage
                : HealPercentage;

            int healAmount = (int)(targetDigimon.MaxHealthPoints * healPercentage * GetHealEffectivenessMultiplier());
            int before = targetDigimon.CurrentHealthPoints;

            targetDigimon.CurrentHealthPoints = Math.Min(
                targetDigimon.MaxHealthPoints,
                targetDigimon.CurrentHealthPoints + healAmount
            );

            return targetDigimon.CurrentHealthPoints - before;
        }

        /// <summary>Vigília (Healer, ver PASSIVAS_SPEC.md): aplica escudo em vez de cura
        /// quando ninguém está ferido (ver SelectTarget). Obedece o mesmo decaimento de R2
        /// (GetHealEffectivenessMultiplier) - "cura, regeneração e escudo de passiva
        /// obedecem o decaimento do Healer".</summary>
        private void ApplyVigiliaShield(BattleUnit targetUnit)
        {
            int shieldAmount = (int)(_target.Digimon.MaxHealthPoints * VigiliaShieldPercentage * GetHealEffectivenessMultiplier());

            _target.ShieldAmount += shieldAmount;

            targetUnit.ShowFloatingText($"+{shieldAmount}", BuffColor);
        }

        /// <summary>
        /// Multiplicador de eficácia da cura (ver BattleCombatant.GetHealEffectivenessMultiplier
        /// pra curva/consts - vive lá porque Fôlego/Aura Vital também precisam dela e não têm
        /// acesso a essa unidade). Usa o tempo real da luta (BattleMatch.ElapsedSeconds), não
        /// o tempo de vida dessa unidade, então vale igual pra quem entrou desde o início.
        /// </summary>
        private float GetHealEffectivenessMultiplier()
        {
            return BattleCombatant.GetHealEffectivenessMultiplier(_arena.Match.ElapsedSeconds);
        }

        // True entre o momento em que o resultado da luta é decidido e a unidade ser
        // destruída (arena fechada) - controla o loop de PlayVictory abaixo.
        private bool _isCelebrating;

        /// <summary>Toca a animação de comemoração ("Happy") em loop - chamado pela
        /// BattleArena nos sobreviventes do time vencedor assim que o resultado é decidido,
        /// e continua repetindo até a unidade ser destruída (jogador saiu da tela de
        /// resultado/batalha). Unidades mortas ficam na pose de derrota, não comemoram.</summary>
        public void PlayVictory()
        {
            if (_isDead || _isCelebrating)
                return;

            _isCelebrating = true;

            _sprite.SetWalking(false);

            _ = _sprite.PlayVictoryLoop(() => _isCelebrating && !_isDead);
        }

        public override void _ExitTree()
        {
            // Sinaliza pro loop de PlayVictory (se estiver rodando) parar de tentar tocar
            // a próxima repetição - a unidade está sendo destruída (arena fechada).
            _isCelebrating = false;
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

            if (_selectionEllipse != null)
                _selectionEllipse.Visible = false;

            SetSelected(false);
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

        /// <summary>
        /// Marca/desmarca essa unidade como a selecionada pelo jogador (clique na arena - ver
        /// BattleArena.HandleUnitSelectionClick) - só liga/desliga o anel branco no chão. O
        /// card com retrato/HP de quem está selecionado é responsabilidade da própria
        /// BattleArena (ver UpdateSelectedUnitPanel), não da unidade.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected && !_isDead;

            if (_selectedRing != null)
                _selectedRing.Visible = _isSelected;
        }
    }
}
