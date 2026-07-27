using ProjetoDC.Scripts.Models.World;
using System;

namespace ProjetoDC.Scripts.Systems.Clock
{
    public class ClockSystem
    {
        private const double SecondsPerGameMinute = 1.0;

        private readonly WorldState _world;
        private double _elapsedSeconds;
        private const int HoursPerDay = 2;

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