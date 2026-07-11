using Godot;


namespace ProjetoDC.Scripts.UI
{
    public partial class DigimonSprite : Control
    {
        public AnimatedSprite2D Sprite;

        public override void _Ready()
        {
            Sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        }

        public void SetDigimon()
        {

        }

        public void PlayIdle()
        {

        }

        public void PlayAttack()
        {

        }

        public void PlayHit()
        {

        }

        public void PlayDeath()
        {

        }
    }
}
