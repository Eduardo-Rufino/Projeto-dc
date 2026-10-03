using ProjetoDC.Enums;

namespace ProjetoDC.Scripts.Data
{
    /// <summary>
    /// Passiva herdada de uma espécie (DigimonData.InheritedPassive): o efeito que um Digimon carrega
    /// pra sempre depois de passar por essa forma (ver DigimonInstance.EvolutionHistory e
    /// InheritedPassiveSystem). O significado de Value/Value2 depende do Type - ver InheritedEffectType.
    /// </summary>
    public class InheritedPassiveData
    {
        public InheritedEffectType Type { get; set; }

        public float Value { get; set; }

        public float Value2 { get; set; }
    }
}
