namespace ProjetoDC.Enums
{
    /// <summary>
    /// Identificador de uma passiva de Digimon (ver PASSIVAS_SPEC.md na raiz do projeto para
    /// a descrição, o orçamento e a razão de cada número). Nomes dos membros seguem o nome em
    /// português da spec, sem acento/espaço. Os efeitos em si (percentuais, cooldowns) NÃO
    /// ficam aqui nem no JSON de <see cref="ProjetoDC.Scripts.Data.PassiveData"/> - vivem como
    /// const perto de onde cada uma é aplicada (BattleUnit/DamageCalculator/BattleCombatant),
    /// mesmo padrão que o resto do sistema de batalha já usa pras próprias constantes.
    /// </summary>
    public enum PassiveType
    {
        // Tank
        Couraca,
        GritoDeGuerra,
        ProvocacaoInsistente,
        Espinhos,
        UltimaTrincheira,
        Folego,

        // Warrior
        Investida,
        GolpePesado,
        FuriaCrescente,
        SedeDeBatalha,
        Teimosia,
        RompeGuarda,

        // Assassin
        PrecisaoLetal,
        FioDaLamina,
        GolpeSorrateiro,
        Execucao,
        Infiltrador,
        PassoFantasma,

        // Ranged
        PosturaFirme,
        PesLeves,
        Fixacao,
        AlvoMarcado,
        ProjetilPerfurante,
        Predicao,

        // Support
        Vigilia,
        MaosFirmes,
        Ressonancia,
        DuplaVoz,
        MarcaDupla,
        InstintoDeFuga,
        AuraVital,
    }
}
