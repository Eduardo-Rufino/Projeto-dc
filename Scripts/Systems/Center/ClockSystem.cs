using ProjetoDC.Scripts.Models.World;
using System;

namespace ProjetoDC.Scripts.Systems.Clock
{
    public class ClockSystem
    {
        // Público: quem precisa saber quanto "tempo real" um pulo de relógio (AdvanceUntilHour)
        // representa - ver GameManager.SkipSleep/DigimonWorld.CatchUpPassiveTime - usa esse
        // fator de conversão em vez de duplicar o número.
        public const double SecondsPerGameMinute = 1.0;

        private readonly WorldState _world;
        private double _elapsedSeconds;
        private const int HoursPerDay = 24;

        public event Action? MinutePassed;
        public event Action? HourPassed;
        public event Action? DayPassed;

        public ClockSystem(WorldState world)
        {
            _world = world;
        }

        public void Update(double delta)
        {
            _elapsedSeconds += delta;

            while (_elapsedSeconds >= SecondsPerGameMinute)
            {
                _elapsedSeconds -= SecondsPerGameMinute;
                AdvanceMinute();
            }
        }

        // Limite de segurança pra AdvanceUntilHour nunca travar num loop infinito (ex.: se
        // chamado com um targetHour que não existe de verdade - HoursPerDay é privado aqui
        // de propósito, então quem chama não tem como validar isso sozinho).
        private const int MaxMinutesPerAdvanceUntilHour = 4 * 60;

        /// <summary>
        /// Avança o relógio minuto a minuto (chamando o mesmo AdvanceMinute que roda em
        /// tempo real) até CurrentHour bater com <paramref name="targetHour"/> - dispara
        /// MinutePassed/HourPassed/DayPassed normalmente a cada passo, então nada que
        /// dependa do relógio passando (fome, stamina, incubação de ovo, doença, save de
        /// fim de dia) é pulado, só a espera em tempo real é que não existe mais. Usado pra
        /// "pular o sono" (ver GameManager.SkipSleep). Retorna quantos minutos de jogo foram
        /// avançados, pra quem chamou saber quanto "tempo real" isso representaria (ver
        /// SecondsPerGameMinute) e compensar sistemas que não são movidos pelo relógio (ex.:
        /// regeneração de HP em DigimonWorld, que ticka em tempo real, não em minutos de jogo).
        /// </summary>
        public int AdvanceUntilHour(int targetHour)
        {
            int minutesAdvanced = 0;

            while (_world.CurrentHour != targetHour && minutesAdvanced < MaxMinutesPerAdvanceUntilHour)
            {
                AdvanceMinute();
                minutesAdvanced++;
            }

            return minutesAdvanced;
        }

        private void AdvanceMinute()
        {
            _world.CurrentMinute++;

            if (_world.CurrentMinute >= 60)
            {
                _world.CurrentMinute = 0;
                _world.CurrentHour++;

                HourPassed?.Invoke();

                if (_world.CurrentHour >= HoursPerDay)
                {
                    _world.CurrentHour = 0;
                    _world.CurrentDay++;

                    DayPassed?.Invoke();
                }
            }

            MinutePassed?.Invoke();
        }
    }
}