using ProjetoDC.Enums;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>
    /// Modificador temporário de um stat durante uma batalha (buff se Percentage > 0,
    /// debuff se Percentage &lt; 0). Vive só em memória, nunca é persistido nem aplicado
    /// ao <see cref="ProjetoDC.Scripts.Gameplay.DigimonInstance.CurrentStats"/>.
    /// </summary>
    public class StatusEffect
    {
        public StatType Stat { get; set; }

        public float Percentage { get; set; }

        public double RemainingSeconds { get; set; }
    }
}
