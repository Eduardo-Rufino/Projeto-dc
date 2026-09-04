using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.UI;
using System;

namespace ProjetoDC.Scripts.Gameplay
{
    /// <summary>
    /// Digimon selvagem numa área de exploração - espécie e nível não são fixos no .tscn, são
    /// sorteados pela ExplorationArea a partir do WildPool do mapa (ver ExplorationArea.
    /// SpawnWilds) toda vez que a área é aberta. Fica vagando perto do próprio ponto de spawn;
    /// se o explorador chegar perto o bastante, passa a persegui-lo, e encostar nele (ou
    /// clicar) dispara a batalha (GameManager.StartWildEncounter). Resolvida a luta (vitória
    /// ou derrota), esse encontro some até a próxima vez que o jogador entrar na área.
    /// CharacterBody2D (não Node2D puro) pra colidir de verdade com a base de árvores/pedras
    /// via MoveAndSlide (ver ForestDecoration) - camada 1, mesma regra do explorador.
    /// </summary>
    public partial class WildEncounterSpawn : CharacterBody2D
    {
        // Vagueia livremente dentro desse raio do próprio spawn, parando um pouco entre um
        // destino e outro (mais orgânico que andar sem parar).
        private const float WanderRadius = 220f;
        private const float WanderMinDistance = 30f;
        private const double WanderPauseMin = 1.0;
        private const double WanderPauseMax = 3.0;
        private const float WanderArrivalThreshold = 4f;

        // A partir dessa distância do explorador, larga o vagueio e vem atrás dele - correndo
        // um pouco mais rápido que o passo normal de vagueio.
        private const float DetectionRadius = 320f;
        private const float ChaseSpeedMultiplier = 1.3f;

        // Distância de "toque" que inicia a batalha sozinha, sem precisar clicar.
        private const float TouchDistance = 28f;

        // Distância máxima do explorador pra um clique manual valer como interação - clicar
        // num selvagem do outro lado da tela não deve batalhar à distância; o clique só
        // "vazar" pra ExplorationDigimon._UnhandledInput (que anda até o ponto clicado) até o
        // jogador chegar perto o bastante (ver OnClickAreaInputEvent).
        private const float ClickInteractionRange = 140f;

        private const float MoveSpeed = 80f;
        private const float FacingDeadZone = 4f;

        private static readonly Random _random = new();

        private DigimonSprite _sprite;
        private int _digimonId;
        private int _level;
        private DigimonInstance _explorer;
        private ExplorationDigimon _explorerVisual;
        private bool _pendingBattle;

        private Vector2 _spawnOrigin;
        private Vector2 _wanderTarget;
        private double _wanderPauseRemaining;

        public override void _Ready()
        {
            _sprite = GetNode<DigimonSprite>("DigimonSprite");

            var clickArea = GetNode<Area2D>("ClickArea");
            clickArea.InputEvent += OnClickAreaInputEvent;

            GameManager.Instance.TeamBattleFinished += OnTeamBattleFinished;
        }

        public override void _ExitTree()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TeamBattleFinished -= OnTeamBattleFinished;
        }

        public void Initialize(
            int digimonId,
            int level,
            DigimonData data,
            DigimonInstance explorer,
            ExplorationDigimon explorerVisual)
        {
            _digimonId = digimonId;
            _level = level;
            _explorer = explorer;
            _explorerVisual = explorerVisual;

            _spawnOrigin = GlobalPosition;
            _wanderTarget = GlobalPosition;

            _sprite.SetDigimon(data.Code);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_pendingBattle || _explorerVisual == null)
            {
                Velocity = Vector2.Zero;
                return;
            }

            float distanceToExplorer = GlobalPosition.DistanceTo(_explorerVisual.GlobalPosition);

            if (distanceToExplorer <= TouchDistance)
            {
                Velocity = Vector2.Zero;
                TriggerBattle();
                return;
            }

            if (distanceToExplorer <= DetectionRadius)
            {
                StepToward(_explorerVisual.GlobalPosition, MoveSpeed * ChaseSpeedMultiplier);
                return;
            }

            ProcessWander(delta);
        }

        private void ProcessWander(double delta)
        {
            if (_wanderPauseRemaining > 0)
            {
                _wanderPauseRemaining -= delta;
                Velocity = Vector2.Zero;
                _sprite.SetWalking(false);
                return;
            }

            if (GlobalPosition.DistanceTo(_wanderTarget) < WanderArrivalThreshold)
            {
                PickNewWanderTarget();
                _wanderPauseRemaining = WanderPauseMin + _random.NextDouble() * (WanderPauseMax - WanderPauseMin);
                Velocity = Vector2.Zero;
                _sprite.SetWalking(false);
                return;
            }

            StepToward(_wanderTarget, MoveSpeed);
        }

        private void PickNewWanderTarget()
        {
            float angle = (float)(_random.NextDouble() * Mathf.Tau);
            float distance = WanderMinDistance + (float)_random.NextDouble() * (WanderRadius - WanderMinDistance);

            _wanderTarget = _spawnOrigin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        }

        private void StepToward(Vector2 target, float speed)
        {
            float facingDelta = target.X - GlobalPosition.X;

            if (Mathf.Abs(facingDelta) > FacingDeadZone)
                _sprite.SetDirection(facingDelta > 0);

            Velocity = GlobalPosition.DirectionTo(target) * speed;
            MoveAndSlide();
            _sprite.SetWalking(true);
        }

        private void OnClickAreaInputEvent(Node viewport, InputEvent @event, long shapeIdx)
        {
            if (_pendingBattle)
                return;

            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                return;

            // Longe demais: não consome o clique - deixa vazar pra ExplorationDigimon.
            // _UnhandledInput, que anda o explorador até o ponto clicado (perto do selvagem)
            // em vez de batalhar à distância.
            if (_explorerVisual == null ||
                GlobalPosition.DistanceTo(_explorerVisual.GlobalPosition) > ClickInteractionRange)
                return;

            GetViewport().SetInputAsHandled();

            TriggerBattle();
        }

        private void TriggerBattle()
        {
            // GameManager.IsBattleActive: recusa por segurança mesmo que isso não devesse
            // ser alcançável (a ExplorationArea inteira para de processar input assim que uma
            // batalha começa - ver ExplorationArea.OnTeamBattleStarted) - evita ficar com
            // _pendingBattle preso em true pra sempre se algum clique perdido ainda chegar
            // aqui antes disso ser desativado.
            if (_pendingBattle || GameManager.Instance.IsBattleActive)
                return;

            _pendingBattle = true;
            _sprite.SetWalking(false);

            GameManager.Instance.StartWildEncounter(_explorer, _digimonId, _level);
        }

        private void OnTeamBattleFinished(BattleResult result)
        {
            if (!_pendingBattle)
                return;

            QueueFree();
        }
    }
}
