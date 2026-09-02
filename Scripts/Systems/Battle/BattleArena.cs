using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
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

        // Raio de tolerância do clique pra selecionar uma unidade (a sprite não tem
        // colisor próprio - mais simples achar a unidade viva mais perto do clique dentro
        // desse raio do que montar Area2D/CollisionShape2D só pra isso).
        private const float SelectionClickRadiusSquared = 34f * 34f;

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

        private BattleUnit _selectedUnit;
        private Control _selectedUnitPanel;
        private TextureRect _selectedUnitPortrait;
        private Label _selectedUnitNameLabel;
        private Label _selectedUnitHpLabel;

        public BattleMatch Match { get; private set; }

        /// <summary>True assim que o resultado da luta é decidido - as BattleUnit usam isso
        /// pra parar de agir imediatamente, em vez de continuar processando/atacando
        /// enquanto a tela de resultado ainda não apareceu.</summary>
        public bool IsBattleOver => _battleEnded;

        public event Action<BattleResult> BattleFinished;

        public override void _Ready()
        {
            // A arena tem sua própria Camera2D com "current = true" no .tscn, mas isso só
            // ativa automaticamente se nenhuma outra câmera já estiver ativa na viewport -
            // como a do Center pode estar ativa (e, com o pan/zoom livre, em qualquer
            // posição/zoom que o jogador tenha deixado antes de entrar em batalha), força
            // explicitamente aqui. Sem isso, a arena era desenhada através da câmera do
            // Center do jeito que ela tivesse sido deixada, ficando descentralizada ou até
            // fora da tela.
            GetNode<Camera2D>("Camera2D").MakeCurrent();

            _unitScene = GD.Load<PackedScene>("res://Scenes/Battle/BattleUnit.tscn");

            _playerSpawnPoints = GetNode<Node2D>("PlayerSpawnPoints");
            _enemySpawnPoints = GetNode<Node2D>("EnemySpawnPoints");
            _unitsContainer = GetNode<Node2D>("UnitsContainer");

            _resultScreen = GetNode<BattleResultScreen>("CanvasLayer/BattleResultScreen");
            _resultScreen.OkPressed += OnResultOkPressed;

            _selectedUnitPanel = GetNode<Control>("CanvasLayer/SelectedUnitPanel");
            _selectedUnitPortrait = GetNode<TextureRect>("CanvasLayer/SelectedUnitPanel/VBoxContainer/Portrait");
            _selectedUnitNameLabel = GetNode<Label>("CanvasLayer/SelectedUnitPanel/VBoxContainer/NameLabel");
            _selectedUnitHpLabel = GetNode<Label>("CanvasLayer/SelectedUnitPanel/VBoxContainer/HPLabel");

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

        /// <summary>Centro geométrico da arena (média das 3 áreas hexagonais) - por construção
        /// (hexágonos mutuamente adjacentes) é sempre um ponto válido dentro de algum deles.
        /// Usado por BattleUnit.MoveTowardTarget como último recurso quando uma unidade fica
        /// travada numa borda/canto sem nenhum passo de movimento válido, garantindo que ela
        /// sempre tem pra onde andar em vez de ficar parada pra sempre.</summary>
        public Vector2 ArenaCenter =>
            _combatAreas.Aggregate(Vector2.Zero, (sum, area) => sum + area.GlobalPosition) / _combatAreas.Count;

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

                // Num 1x1, usa o ponto do meio (índice 1) em vez do primeiro - os 3 pontos
                // são pensados como flanco/centro/flanco, então nascer sempre no índice 0
                // deixaria o único Digimon torto pro lado em vez de centralizado.
                int pointIndex = team.Members.Count == 1 && points.Count > 1 ? 1 : i;

                if (pointIndex < points.Count)
                    unit.GlobalPosition = points[pointIndex].GlobalPosition;

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

        /// <summary>
        /// Clique do jogador em qualquer unidade (aliada ou inimiga) da arena, viva, seleciona
        /// ela - o card no canto (ver UpdateSelectedUnitPanel) mostra o retrato e o HP: exato
        /// pro time do jogador, só porcentagem pro time inimigo. Clicar fora de qualquer
        /// unidade desmarca a seleção atual.
        /// </summary>
        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                return;

            HandleUnitSelectionClick(GetGlobalMousePosition());
        }

        private void HandleUnitSelectionClick(Vector2 worldPosition)
        {
            BattleUnit closest = null;
            float closestDistanceSquared = SelectionClickRadiusSquared;

            foreach (var unit in _unitsByCombatant.Values)
            {
                if (unit.IsDead)
                    continue;

                float distanceSquared = unit.GlobalPosition.DistanceSquaredTo(worldPosition);

                if (distanceSquared <= closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    closest = unit;
                }
            }

            if (closest == _selectedUnit)
                return;

            _selectedUnit?.SetSelected(false);
            _selectedUnit = closest;
            _selectedUnit?.SetSelected(true);

            UpdateSelectedUnitPanel();
        }

        /// <summary>
        /// Card no canto da tela com retrato + HP da unidade selecionada (ver
        /// HandleUnitSelectionClick) - some sozinho se a unidade morrer, já que não faz
        /// sentido continuar mostrando o card de alguém que não está mais na luta.
        /// </summary>
        private void UpdateSelectedUnitPanel()
        {
            if (_selectedUnit != null && _selectedUnit.IsDead)
            {
                _selectedUnit = null;
            }

            if (_selectedUnit == null)
            {
                _selectedUnitPanel.Visible = false;
                return;
            }

            var digimon = _selectedUnit.Combatant.Digimon;

            _selectedUnitPanel.Visible = true;
            _selectedUnitNameLabel.Text = digimon.DisplayName;

            string portraitPath = $"res://Assets/Sprites/Digimon/{digimon.BaseData.Code}.png";

            _selectedUnitPortrait.Texture = ResourceLoader.Exists(portraitPath)
                ? GD.Load<Texture2D>(portraitPath)
                : null;

            if (_selectedUnit.IsPlayerSide)
            {
                _selectedUnitHpLabel.Text = $"{digimon.CurrentHealthPoints}/{digimon.MaxHealthPoints}";
            }
            else
            {
                float pct = digimon.MaxHealthPoints <= 0
                    ? 0f
                    : (float)digimon.CurrentHealthPoints / digimon.MaxHealthPoints;

                _selectedUnitHpLabel.Text = $"{Mathf.RoundToInt(Mathf.Clamp(pct, 0f, 1f) * 100f)}%";
            }
        }

        public override void _Process(double delta)
        {
            if (_selectedUnit != null)
                UpdateSelectedUnitPanel();

            if (Match == null || _resultShown)
                return;

            if (_battleEnded)
            {
                _resultDelayRemaining -= delta;

                if (_resultDelayRemaining <= 0)
                {
                    _resultShown = true;

                    // Mesmo cálculo que GameManager.ApplyTeamBattleReward vai aplicar de
                    // verdade quando o jogador apertar OK - só pra mostrar o número já na
                    // tela de resultado. Nada aqui muda o estado do time, então recalcular
                    // de novo lá na hora de aplicar dá o mesmo valor.
                    BattleReward reward = null;

                    // Campeonato não dá XP (ver GameManager.ApplyTeamBattleReward) - a
                    // prévia precisa refletir isso, senão mostra um número que nunca é
                    // aplicado de verdade.
                    bool isTournament = GameManager.Instance.ActiveTournament != null;

                    if (_pendingResult == BattleResult.PlayerWon)
                    {
                        reward = BattleRewardCalculator.CalculateForTeam(
                            Match.PlayerTeam.Members.Select(m => m.Digimon).ToList(),
                            Match.EnemyTeam.Members.Select(m => m.Digimon).ToList());

                        if (isTournament)
                            reward.Experience = 0;

                        // Se for a primeira vitória num campeonato, mostra a capacidade
                        // ganha junto - ApplyTournamentCapacityRewardIfNeeded concede a
                        // mesma coisa de verdade quando o jogador apertar OK.
                        var tournament = GameManager.Instance.ActiveTournament;

                        if (tournament != null &&
                            !GameManager.Instance.Save.Center.ClearedTournamentIds.Contains(tournament.Id))
                        {
                            reward.CapacityGained = tournament.CapacityReward;
                            reward.BonusBitsGained = tournament.BitsReward;
                        }
                    }
                    else if (_pendingResult == BattleResult.EnemyWon)
                    {
                        // Mesmo perdendo, ApplyTeamBattleReward concede uma fração de
                        // consolação (ver GameManager.LossExperienceDivisor) - mostra
                        // aqui pro jogador ver o que ganhou mesmo na derrota. Campeonato
                        // não dá esse XP de consolação também.
                        int consolationXp = 0;

                        if (!isTournament)
                        {
                            var fullReward = BattleRewardCalculator.CalculateForTeam(
                                Match.PlayerTeam.Members.Select(m => m.Digimon).ToList(),
                                Match.EnemyTeam.Members.Select(m => m.Digimon).ToList());

                            consolationXp = fullReward.Experience / GameManager.LossExperienceDivisor;
                        }

                        reward = new BattleReward
                        {
                            Experience = consolationXp,
                            Bits = 0
                        };
                    }

                    _resultScreen.ShowResult(_pendingResult, reward);
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

            PlayVictoryAnimation(result);
        }

        /// <summary>Toca "Happy" nos sobreviventes do time vencedor assim que o resultado é
        /// decidido, durante o intervalo (ResultDelaySeconds) antes da tela de resultado
        /// aparecer - sem efeito em caso de empate/luta indecisa (não deveria acontecer,
        /// CheckResult só retorna PlayerWon/EnemyWon aqui).</summary>
        private void PlayVictoryAnimation(BattleResult result)
        {
            BattleTeam winningTeam = result == BattleResult.PlayerWon
                ? Match.PlayerTeam
                : result == BattleResult.EnemyWon
                    ? Match.EnemyTeam
                    : null;

            if (winningTeam == null)
                return;

            foreach (var member in winningTeam.Members)
            {
                if (_unitsByCombatant.TryGetValue(member, out var unit))
                    unit.PlayVictory();
            }
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
