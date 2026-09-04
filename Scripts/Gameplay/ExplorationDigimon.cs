using Godot;
using ProjetoDC.Scripts.UI;

namespace ProjetoDC.Scripts.Gameplay
{
    /// <summary>
    /// Controlador leve do Digimon que está explorando (ver ExplorationArea) - sem nenhuma
    /// bagagem do Center (arrastar, fome, sono, dormitório): anda até onde o jogador clicar
    /// OU direto via WASD/setas, sem sair do limite do mapa. Interativos (selvagem/NPC/item)
    /// marcam o próprio clique como tratado (GetViewport().SetInputAsHandled()), então clicar
    /// neles não também manda o explorador andar até lá.
    /// CharacterBody2D (não Node2D puro) pra colidir de verdade com a base de árvores/pedras
    /// via MoveAndSlide (ver ForestDecoration) - a máscara 1 é exatamente essa camada de
    /// obstáculo; o limite do mapa em si continua sendo um checagem manual (IsPointInsideBoundary),
    /// não física, porque o Boundary é só um Polygon2D visual.
    /// </summary>
    public partial class ExplorationDigimon : CharacterBody2D
    {
        private const float ArrivalThreshold = 2f;
        private const float FacingDeadZone = 4f;

        [Export] public float Speed { get; set; } = 130f;

        private DigimonSprite _sprite;
        private ExplorationArea _area;
        private Vector2 _targetPosition;
        private bool _isWalking;
        private bool _isKeyboardMoving;

        public override void _Ready()
        {
            _sprite = GetNode<DigimonSprite>("DigimonSprite");
            _targetPosition = GlobalPosition;
        }

        public void Initialize(DigimonInstance digimon, ExplorationArea area)
        {
            _area = area;
            _sprite.SetDigimon(digimon.BaseData.Code);
        }

        public void MoveTo(Vector2 globalPosition)
        {
            if (_area != null && !_area.IsPointInsideBoundary(globalPosition))
                return;

            _targetPosition = globalPosition;
            _isWalking = true;
            _sprite.SetWalking(true);
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                MoveTo(GetGlobalMousePosition());
        }

        public override void _PhysicsProcess(double delta)
        {
            Vector2 keyboardDirection = GetKeyboardDirection();

            if (keyboardDirection != Vector2.Zero)
            {
                // WASD/setas têm prioridade sobre um destino de clique pendente - segurar uma
                // tecla cancela o clique-pra-andar em vez de competir com ele.
                _isWalking = false;
                _isKeyboardMoving = true;

                ProcessKeyboardMovement(keyboardDirection);
                return;
            }

            if (_isKeyboardMoving)
            {
                _isKeyboardMoving = false;
                Velocity = Vector2.Zero;
                _sprite.SetWalking(false);
                _sprite.PlayIdle();
            }

            if (!_isWalking)
            {
                Velocity = Vector2.Zero;
                return;
            }

            float facingDelta = _targetPosition.X - GlobalPosition.X;

            if (Mathf.Abs(facingDelta) > FacingDeadZone)
                _sprite.SetDirection(facingDelta > 0);

            if (GlobalPosition.DistanceTo(_targetPosition) < ArrivalThreshold)
            {
                _isWalking = false;
                Velocity = Vector2.Zero;
                _sprite.SetWalking(false);
                _sprite.PlayIdle();
                return;
            }

            Velocity = GlobalPosition.DirectionTo(_targetPosition) * Speed;
            MoveAndSlide();
        }

        private static Vector2 GetKeyboardDirection()
        {
            Vector2 direction = Vector2.Zero;

            if (Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.Up))
                direction.Y -= 1;

            if (Input.IsPhysicalKeyPressed(Key.S) || Input.IsPhysicalKeyPressed(Key.Down))
                direction.Y += 1;

            if (Input.IsPhysicalKeyPressed(Key.A) || Input.IsPhysicalKeyPressed(Key.Left))
                direction.X -= 1;

            if (Input.IsPhysicalKeyPressed(Key.D) || Input.IsPhysicalKeyPressed(Key.Right))
                direction.X += 1;

            return direction.Normalized();
        }

        /// <summary>Checa o limite do mapa nos dois eixos separadamente (antes de aplicar a
        /// física) pra poder deslizar ao longo de uma borda em vez de travar totalmente quando
        /// só um dos eixos ainda cabe dentro do limite. MoveAndSlide ainda cuida da colisão
        /// física contra árvore/pedra em cima disso.</summary>
        private void ProcessKeyboardMovement(Vector2 direction)
        {
            Vector2 velocity = direction * Speed;

            if (_area != null)
            {
                float probeDistance = Speed * 0.1f;
                Vector2 afterX = GlobalPosition + new Vector2(direction.X, 0) * probeDistance;
                Vector2 afterY = GlobalPosition + new Vector2(0, direction.Y) * probeDistance;

                if (direction.X != 0 && !_area.IsPointInsideBoundary(afterX))
                    velocity.X = 0;

                if (direction.Y != 0 && !_area.IsPointInsideBoundary(afterY))
                    velocity.Y = 0;
            }

            if (Mathf.Abs(direction.X) > 0.01f)
                _sprite.SetDirection(direction.X > 0);

            Velocity = velocity;
            MoveAndSlide();

            _targetPosition = GlobalPosition;
            _sprite.SetWalking(velocity != Vector2.Zero);
        }
    }
}
