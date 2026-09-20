namespace ProjetoDC.Enums
{
    /// <summary>Tipo de uma ligação da teia evolutiva (Data/Evolution/evolution_web.json).</summary>
    public enum EvolutionLinkType
    {
        /// <summary>Evolução comum, sempre para o estágio seguinte.</summary>
        Normal,

        /// <summary>DNA Digivolution (Jogress): o resultado exige os dois Digimons parceiros.</summary>
        Dna,

        /// <summary>Troca de modo dentro do mesmo estágio (ex.: Belphemon Sleep Mode → Rage Mode).</summary>
        Mode,
    }
}
