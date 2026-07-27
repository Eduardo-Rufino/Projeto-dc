using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.UI
{
    public partial class DigimonSprite : Node2D
    {
        public AnimatedSprite2D _sprite;

        private string _currentCode = "";

        public override void _Ready()
        {
            _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        }

        public void SetDigimon(string code)
        {
            if (_currentCode == code && _sprite.SpriteFrames != null)
                return;

            _currentCode = code;

            var frames = new SpriteFrames();

            AddAnimation(frames, code, "Idle", 0, 1);
            AddAnimation(frames, code, "Happy", 0, 2);
            AddAnimation(frames, code, "Angry", 0, 3);
            AddAnimation(frames, code, "Attack", 4, 5);

            // Por enquanto vou assumir que 6 e 7 são a animação de treino
            AddAnimation(frames, code, "Train", 6, 7);

            AddAnimation(frames, code, "Eat", 8, 9);
            AddAnimation(frames, code, "Refuse", 0, 10);
            AddAnimation(frames, code, "Sleep", 11, 12);
            AddAnimation(frames, code, "Sick", 13);
            AddAnimation(frames, code, "Hit", 13);
            AddAnimation(frames, code, "SickLose", 14);

            _sprite.SpriteFrames = frames;
        }

        public void RefreshState(DigimonInstance digimon)
        {
            if (digimon.HealthState == HealthState.Sick)
            {
                PlaySick();
                return;
            }

            switch (digimon.Activity)
            {
                case DigimonActivity.Sleeping:
                    PlaySleep();
                    break;

                case DigimonActivity.Training:
                    PlayTrain();
                    break;

                default:
                    PlayIdle();
                    break;
            }
        }

        /*
        private SpriteFrames LoadIdleFrames(string code)
        {
            var frames = new SpriteFrames();

            frames.AddAnimation("Idle");
            frames.SetAnimationLoopMode("Idle", SpriteFrames.LoopMode.Linear);
            frames.SetAnimationSpeed("Idle", 2);

            string folder = $"res://Assets/Sprites/Digimon/{code}/";

            for (int i = 0; i <= 1; i++)
            {
                string path = $"{folder}{i}.png";

                if (ResourceLoader.Exists(path))
                {
                    frames.AddFrame("Idle", GD.Load<Texture2D>(path));
                }
            }

            return frames;
        }
        */

        private void AddAnimation(SpriteFrames frames, string code, string animation, params int[] frameIndexes)
        {
            frames.AddAnimation(animation);
            frames.SetAnimationSpeed(animation, 2);

            if (animation == "Idle" || animation == "Happy" || animation == "Sleep")
            {
                frames.SetAnimationLoopMode(animation, SpriteFrames.LoopMode.Linear);
            }
            else
            {
                frames.SetAnimationLoopMode(animation, SpriteFrames.LoopMode.None);
            }

            string folder = $"res://Assets/Sprites/Digimon/{code}/";

            foreach (int frameIndex in frameIndexes)
            {
                string path = $"{folder}{frameIndex}.png";

                if (ResourceLoader.Exists(path))
                {
                    var texture = GD.Load<Texture2D>(path);
                    frames.AddFrame(animation, texture);
                }
                else
                {
                    GD.PrintErr($"Frame não encontrado: {path}");
                }
            }
        }

        public async Task PlayTemporary(string animation, int loops = 1)
        {
            for (int i = 0; i < loops; i++)
            {
                _sprite.Play(animation);

                await ToSignal(_sprite, AnimatedSprite2D.SignalName.AnimationFinished);
            }

            PlayIdle();
        }

        public async Task PlayEat(int loops = 2)
        {
            for (int i = 0; i < loops; i++)
            {
                _sprite.Play("Eat");

                await ToSignal(_sprite, AnimatedSprite2D.SignalName.AnimationFinished);
            }

            PlayIdle();
        }

        public async Task PlayRefuse(int loops = 2)
        {
            for (int i = 0; i < loops; i++)
            {
                _sprite.Play("Refuse");
                await ToSignal(_sprite, AnimatedSprite2D.SignalName.AnimationFinished);
            }

            PlayIdle();
        }

        public void SetFlip(bool flipped)
        {
            _sprite.FlipH = flipped;
        }

        public void Play(string animation)
        {
            _sprite.Play(animation);
        }

        public void PlayIdle()
        {
            _sprite.Play("Idle");
        }

        public void PlaySleep()
        {
            _sprite.Play("Sleep");
        }

        public void PlayHappy()
        {
            _sprite.Play("Happy");
        }

        public void PlayAngry()
        {
            _sprite.Play("Angry");
        }

        public void PlaySick()
        {
            _sprite.Play("Sick");
        }

        public void PlaySickLose()
        {
            _sprite.Play("SickLose");
        }

        public async void PlayAttack()
        {
            _sprite.Play("Attack");

            await ToSignal(
                _sprite,
                AnimatedSprite2D.SignalName.AnimationFinished
            );

            PlayIdle();
        }

        public async void PlayHit()
        {
            _sprite.Play("Hit");

            await ToSignal(
                _sprite,
                AnimatedSprite2D.SignalName.AnimationFinished
            ); 
            
            PlayIdle();

        }

        public void PlayDeath()
        {
            _sprite.Play("SickLose");
        }

        public void PlayVictory()
        {
            _sprite.Play("Happy");
        }

        public void PlayTrain()
        {
            _sprite.Play("Train");
        }
    }
}
