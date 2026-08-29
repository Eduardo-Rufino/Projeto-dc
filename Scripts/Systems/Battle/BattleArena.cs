using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>
    /// Arena de uma batalha 3x3 em tempo real: instancia as 6 BattleUnit, guarda o
    /// BattleMatch (estado puro) e detecta o fim da luta.
    /// </summary>
    public partial class BattleArena : Node2D
    {
        private const float ResultDelaySeconds = 1.5f;

        // Posições dos 3 hexágonos (mutuamente adjacentes) que delimitam a área de combate,
        // centralizados na tela da arena.
        private static readonly Vector2[] HexPositions =
        {
            new(452.5f, 360f),
            new(827.5f, 576.5f),
            new(827.5f, 143.5f),
        };

        private PackedScene _unitScene;

        private Node2D _playerSpawnPoints;
        private Node2D _enemySpawnPoints;
        private Node2D _unitsContainer;
        private BattleResultScreen _resultScreen;
        private List<CenterArea> _combatAreas;

        private readonly Dictionary<BattleCombatant, BattleUnit> _unitsByCombatant = new();

        private bool _battleEnded;
        private bool _resultShown;
        private double _resultDelayRemaining;
        private BattleResult _pendingResult;

        public BattleMatch Match { get; private set; }

        /// <summary>True assim que o resultado da luta é decidido - as BattleUnit usam isso
        /// pra parar de agir imediatamente, em vez de continuar processando/atacando
        /// enquanto a tela de resultado ainda não apareceu.</summary>
        public bool IsBattleOver => _battleEnded;

        public event Action<BattleResult> BattleFinished;

        public override void _Ready()
        {
            _unitScene = GD.Load<PackedScene>("res://Scenes/Battle/BattleUnit.tscn");

            _playerSpawnPoints = GetNode<Node2D>("PlayerSpawnPoints");
            _enemySpawnPoints = GetNode<Node2D>("EnemySpawnPoints");
            _unitsContainer = GetNode<Node2D>("UnitsContainer");

            _resultScreen = GetNode<BattleResultScreen>("CanvasLayer/BattleResultScreen");
            _resultScreen.OkPressed += OnResultOkPressed;

            _combatAreas = GetNode<Node2D>("CombatAreas")
                .GetChildren()
                .OfType<CenterArea>()
                .ToList();

            // CenterArea._Ready() sobrescreve a própria Position com base em GridPosition
            // (usando o Origin fixo do CenterGrid, pensado pro Center principal) - como já
            // rodou nesse ponto (nós filhos ficam prontos antes do pai), reposicionamos os
            // hexágonos aqui pro layout específico da arena de batalha.
            for (int i = 0; i < _combatAreas.Count && i < HexPositions.Length; i++)
            {
                _combatAreas[i].Position = HexPositions[i];
            }
        }

        /// <summary>Verifica se um ponto está dentro de algum dos hexágonos que delimitam
        /// a arena (o jogador pode andar livremente entre eles, mas não pra fora).</summary>
        public bool IsPointInsideArena(Vector2 globalPosition)
        {
            return _combatAreas.Any(area => area.IsPointInside(globalPosition));
        }

        public void Init(List<DigimonInstance> playerTeam, List<DigimonInstance> enemyTeam)
        {
            // O time do jogador entra na luta com o HP atual (reflete o estado real do
            // Digimon); só os inimigos, recém-criados pro combate, começam com HP cheio.
            foreach (var digimon in enemyTeam)
                digimon.RestoreHealth();

            var playerBattleTeam = new BattleTeam(playerTeam);
            var enemyBattleTeam = new BattleTeam(enemyTeam);

            Match = new BattleMatch(playerBattleTeam, enemyBattleTeam);

            SpawnTeam(playerBattleTeam, _playerSpawnPoints, true);
            SpawnTeam(enemyBattleTeam, _enemySpawnPoints, false);
        }

        private void SpawnTeam(BattleTeam team, Node2D spawnPoints, bool isPlayerSide)
        {
            var points = spawnPoints.GetChildren().OfType<Marker2D>().ToList();

            for (int i = 0; i < team.Members.Count; i++)
            {
                var unit = _unitScene.Instantiate<BattleUnit>();

                _unitsContainer.AddChild(unit);

                if (i < points.Count)
                    unit.GlobalPosition = points[i].GlobalPosition;

                unit.Initialize(team.Members[i], isPlayerSide, this);

                _unitsByCombatant[team.Members[i]] = unit;
            }
        }

        public BattleUnit GetUnitFor(BattleCombatant combatant)
        {
            _unitsByCombatant.TryGetValue(combatant, out var unit);

            return unit;
        }

        /// <summary>Adiciona um nó (ex.: projétil de ataque à distância) ao mundo 2D da arena.</summary>
        public void AddToWorld(Node2D node)
        {
            _unitsContainer.AddChild(node);
        }

        public override void _Process(double delta)
        {
            if (Match == null || _resultShown)
                return;

            if (_battleEnded)
            {
                _resultDelayRemaining -= delta;

                if (_resultDelayRemaining <= 0)
                {
                    _resultShown = true;
                    _resultScreen.ShowResult(_pendingResult);
                }

                return;
            }

            Match.Tick(delta);

            var result = Match.CheckResult();

            if (result == BattleResult.Ongoing)
                return;

            _battleEnded = true;
            _pendingResult = result;
            _resultDelayRemaining = ResultDelaySeconds;
        }

        private void OnResultOkPressed()
        {
            // Proteção: só encerra a batalha de verdade se o resultado já foi
            // decidido pelo Match (evita terminar a luta com um resultado falso
            // caso a tela de resultado seja exibida/clicada antes da hora).
            if (!_resultShown)
                return;

            BattleFinished?.Invoke(_pendingResult);
        }
    }
}
