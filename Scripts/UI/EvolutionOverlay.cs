using Godot;
using ProjetoDC.Scripts.Data;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Overlay de tela cheia tocado durante uma evolução: mostra um sprite grande do Digimon
    /// centralizado na tela, alternando entre a forma antiga e a nova (com um flash claro a
    /// cada troca) antes de assentar na forma nova. Quem enfileira e pausa o resto do jogo
    /// é o Center (ver Center.EnqueueEvolution/ProcessEvolutionQueue) - esse script só cuida
    /// da própria animação.
    /// </summary>
    public partial class EvolutionOverlay : CanvasLayer
    {
        private const double FlickerInterval = 0.15;
        private const int FlickerCount = 6;
        private const double FinalFlashSeconds = 0.35;
        private const double HoldAfterSeconds = 0.4;

        private static readonly Color FlashColor = new(3.5f, 3.5f, 3.5f, 1f);

        private Label _nameLabel;
        private DigimonSprite _sprite;

        public override void _Ready()
        {
            // Precisa continuar rodando (Tweens/timers inclusos) mesmo com a árvore pausada -
            // é o próprio Center quem pausa o resto do jogo enquanto essa animação toca.
            ProcessMode = ProcessModeEnum.Always;

            _nameLabel = GetNode<Label>("CenterAnchor/NameLabel");
            _sprite = GetNode<DigimonSprite>("CenterAnchor/SpriteRoot/DigimonSprite");

            Visible = false;
        }

        public async Task Play(DigimonData oldForm, DigimonData newForm)
        {
            Visible = true;

            _sprite._sprite.Modulate = Colors.White;

            _sprite.SetDigimon(oldForm.Code);
            _nameLabel.Text = oldForm.Name;

            bool showingNewForm = false;

            for (int i = 0; i < FlickerCount; i++)
            {
                showingNewForm = !showingNewForm;

                var form = showingNewForm ? newForm : oldForm;

                _sprite.SetDigimon(form.Code);
                _nameLabel.Text = form.Name;

                await FlashSprite(FlickerInterval);
            }

            _sprite.SetDigimon(newForm.Code);
            _nameLabel.Text = newForm.Name;

            // Flash final mais demorado, marcando a forma definitiva.
            await FlashSprite(FinalFlashSeconds);

            await ToSignal(
                GetTree().CreateTimer(HoldAfterSeconds),
                SceneTreeTimer.SignalName.Timeout
            );

            Visible = false;
        }

        /// <summary>
        /// Sobe o Modulate do sprite pra um valor bem acima de 1.0 e volta - com um sprite
        /// comum (sem material especial), multiplicar a cor por um valor alto estoura os
        /// canais pra perto do branco na tela, dando o efeito de "flash" sem precisar de
        /// shader.
        /// </summary>
        private async Task FlashSprite(double totalSeconds)
        {
            var animatedSprite = _sprite._sprite;

            double half = totalSeconds / 2.0;

            Tween tween = CreateTween();

            tween.TweenProperty(animatedSprite, "modulate", FlashColor, half)
                .SetTrans(Tween.TransitionType.Sine);

            tween.TweenProperty(animatedSprite, "modulate", Colors.White, half)
                .SetTrans(Tween.TransitionType.Sine);

            await ToSignal(tween, Tween.SignalName.Finished);
        }
    }
}
