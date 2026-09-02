using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Enums
{
    public enum CenterAreaType {
        Neutral,
        Training,
        Dormitory,
        Restaurant,
        Hospital,

        // Áreas de treino especializadas (vendidas na loja - ver GameManager.
        // SpecificTrainingAreaPrice) - cada uma força o mesmo stat de TrainingType sempre que
        // um Digimon treina nela, com um bônus leve sobre o treino genérico (ver CenterArea.
        // GetForcedTrainingType / TrainingSystem.SpecificAreaBonusMultiplier).
        TrainingHealthPoints,
        TrainingAttack,
        TrainingDefense,
        TrainingSpecialAttack,
        TrainingSpecialDefense,
        TrainingSpeed,
    }
}
