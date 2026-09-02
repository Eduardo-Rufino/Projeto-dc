using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Gameplay;
using System;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.UI
{
    public partial class DigimonSprite : Node2D
    {
        public AnimatedSprite2D _sprite;
        private bool _walking;

        public string CurrentAnimation =>
             _sprite.Animation.ToString();

        public int CurrentFrame =>
            _sprite.Frame;

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

            _sprite.Stop();
            _sprite.Frame = 0;
            _sprite.Play("Idle");
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
                    _ = PlayTrainingSequence(1);
                    break;

                case DigimonActivity.Eating:
                    PlayEat();
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

            if (animation == "Idle" || animation == "Sleep" || animation == "Eat")
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

        public async Task PlayRefuse(int loops = 2)
        {
            for (int i = 0; i < loops; i++)
            {
                _sprite.Play("Refuse");
                await ToSignal(_sprite, AnimatedSprite2D.SignalName.AnimationFinished);
            }

            PlayIdle();
        }

        public void SetDirection(bool lookingLeft)
        {
            _sprite.FlipH = lookingLeft;
        }

        public void SetWalking(bool walking)
        {
            if (_walking == walking)
                return;

            _walking = walking;

            _sprite.SpeedScale = walking ? 1.4f : 1.0f;
        }

        public void Play(string animation)
        {
            _sprite.Play(animation);
        }

        /// <summary>Usado pela batalha pra saber, quadro a quadro, se a animação de ataque
        /// ainda está tocando - permite cortar pra Idle antes do fim caso o alvo morra no
        /// meio do golpe, em vez de esperar o AnimationFinished.</summary>
        public bool IsPlaying()
        {
            return _sprite.IsPlaying();
        }

        /// <summary>True se <paramref name="animation"/> for a animação tocando agora. Como
        /// o AnimatedSprite2D é compartilhado, outra coisa pode assumir o sprite no meio de
        /// uma espera (ex.: essa unidade toma um golpe enquanto ainda está no meio do próprio
        /// ataque) - checar pelo nome, e não só "tem alguma coisa tocando", é o que permite
        /// quem estava esperando essa animação específica perceber a troca e não ficar preso
        /// esperando algo que nunca mais vai terminar (ex.: se o que assumiu foi um Idle, que
        /// fica em loop pra sempre).</summary>
        public bool IsPlayingAnimation(string animation)
        {
            return _sprite.Animation == animation && _sprite.IsPlaying();
        }

        public void PlayIdle()
        {
            if (_sprite.Animation == "Idle" && _sprite.IsPlaying())
                return;

            _sprite.Play("Idle");
        }

        public void PlayEat()
        {
            _sprite.Play("Eat");
        }

        public void PlaySleep()
        {
            _sprite.Play("Sleep");
        }

        public void PlayHappy()
        {
            if (_sprite.Animation == "Happy" && _sprite.IsPlaying())
                return;

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

        public async Task PlayAttack()
        {
            _sprite.Play("Attack");

            await ToSignal(
                _sprite,
                AnimatedSprite2D.SignalName.AnimationFinished
            );

            PlayIdle();
        }

        /// <summary>
        /// Toca a animação de dano quadro a quadro (em vez de esperar cegamente o
        /// AnimationFinished): se essa unidade também estiver no meio do próprio ataque e o
        /// sprite for retomado pra "Attack" antes do "Hit" terminar, ou se for pra Idle/Sleep
        /// (que ficam em loop e nunca disparam AnimationFinished sozinhos), esperar o sinal
        /// original travaria pra sempre. Poll por "ainda é Hit que está tocando" evita isso.
        /// </summary>
        public async Task PlayHit()
        {
            _sprite.Play("Hit");

            while (IsPlayingAnimation("Hit"))
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            // Só força Idle se "Hit" realmente terminou sozinho - se outra coisa já assumiu o
            // sprite nesse meio tempo (a própria unidade começou a atacar, por exemplo), não
            // pisa em cima disso.
            if (_sprite.Animation == "Hit")
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

        /// <summary>Toca "Happy" em loop, reiniciando do frame 0 a cada ciclo, enquanto
        /// <paramref name="shouldContinue"/> retornar true - "Happy" é LoopMode.None (ver
        /// AddAnimation), então sem isso ela tocaria só uma vez e pararia no último frame.
        /// Usado na comemoração de vitória em batalha, que deve continuar até o jogador
        /// sair da tela de resultado. Para sozinha se algo externo assumir o sprite
        /// (ex.: a unidade sendo destruída/liberada nesse meio tempo).</summary>
        public async Task PlayVictoryLoop(Func<bool> shouldContinue)
        {
            while (shouldContinue())
            {
                _sprite.Stop();
                _sprite.Frame = 0;
                _sprite.Play("Happy");

                if (!await WaitForOwnAnimation("Happy"))
                    return;
            }
        }

        public void PlayTrain()
        {
            if (_sprite.Animation == "Train" && _sprite.IsPlaying())
                return;

            _sprite.Play("Train");
        }

        /// <summary>
        /// Toca a sequência de treino (N repetições de "Train" + 2 de "Happy") quadro a
        /// quadro, igual a PlayAttackWatchingTarget/PlayHit, em vez de esperar cegamente
        /// AnimationFinished: se o Digimon dormir (ou ficar doente) no meio do treino, algo
        /// externo troca o sprite compartilhado pra "Sleep"/"Sick" antes da animação atual
        /// terminar sozinha, e o AnimationFinished daquele "Train"/"Happy" específico nunca
        /// mais dispara - travando quem esperava pra sempre (era o bug: Digimon acordava
        /// preso, sem fazer nenhuma ação, até o jogo ser reiniciado). Retorna false se foi
        /// interrompida assim, pra quem chamou saber que não deve retomar o comportamento
        /// normal (ex.: sair andando de novo) por cima do que interrompeu.
        /// </summary>
        public async Task<bool> PlayTrainingSequence(int trainingLoops)
        {
            for (int i = 0; i < trainingLoops; i++)
            {
                _sprite.Stop();
                _sprite.Frame = 0;
                _sprite.Play("Train");

                if (!await WaitForOwnAnimation("Train"))
                    return false;
            }

            for (int i = 0; i < 2; i++)
            {
                _sprite.Stop();
                _sprite.Frame = 0;
                _sprite.Play("Happy");

                if (!await WaitForOwnAnimation("Happy"))
                    return false;
            }

            PlayIdle();

            return true;
        }

        /// <summary>Espera <paramref name="animation"/> terminar sozinha (LoopMode.None,
        /// então IsPlaying() vira false no fim natural). Retorna false se outra coisa assumiu
        /// o sprite antes disso (o nome da animação atual mudou).</summary>
        private async Task<bool> WaitForOwnAnimation(string animation)
        {
            while (IsPlayingAnimation(animation))
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            return _sprite.Animation == animation;
        }
    }
}
