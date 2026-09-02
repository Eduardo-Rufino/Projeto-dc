using Godot;
using System;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>
    /// Sprite de ataque à distância que viaja do atacante até o alvo. Usado só por
    /// ataques ranged (Role.Ranged) - ataques corpo a corpo não precisam disso.
    /// </summary>
    public partial class AttackProjectile : Node2D
    {
        // Público pra quem disparou poder calcular quanto tempo o voo leva (usado pra saber
        // quanta margem de movimento incidental o alvo teve durante o trajeto, ao decidir se
        // um golpe foi esquivado).
        public const float TravelSpeed = 300f;
        private const float ArrivalThreshold = 6f;

        private Sprite2D _sprite;
        private Vector2 _targetPosition;
        private bool _arrived;

        public event Action Arrived;

        public override void _Ready()
        {
            _sprite = GetNode<Sprite2D>("Sprite2D");
        }

        public void Launch(Vector2 from, Vector2 to, Texture2D texture)
        {
            GlobalPosition = from;
            _targetPosition = to;

            _sprite.Texture = texture;
            Rotation = (to - from).Angle();
        }

        public override void _Process(double delta)
        {
            if (_arrived)
                return;

            GlobalPosition = GlobalPosition.MoveToward(
                _targetPosition,
                TravelSpeed * (float)delta
            );

            if (GlobalPosition.DistanceTo(_targetPosition) <= ArrivalThreshold)
            {
                _arrived = true;

                Arrived?.Invoke();

                QueueFree();
            }
        }
    }
}
