using System.Collections.Generic;
using ProjetoDC.Enums;

namespace ProjetoDC.Scripts.Data
{
    /// <summary>
    /// Metadados de uma passiva (Sistema de Passivas, ver PASSIVAS_SPEC.md na raiz do
    /// projeto) - nome/descrição pra UI e a Role/SupportTypes exigidos pra uma espécie poder
    /// incluí-la no próprio pool (ver DigimonData.Passives). Os números de efeito (percentuais,
    /// cooldowns, limiares) não ficam aqui - vivem como const em C#, perto de onde cada
    /// passiva é de fato aplicada.
    /// </summary>
    public class PassiveData
    {
        public PassiveType Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>Role à qual essa passiva se aplica - só espécies dessa Role podem tê-la
        /// no pool.</summary>
        public RoleType Role { get; set; }

        /// <summary>Só relevante quando Role == Support. Vazia significa "qualquer subtipo"
        /// (ex.: Aura Vital); com 1 ou mais valores, restringe aos subtipos listados (ex.:
        /// Instinto de Fuga = [Healer, Buffer], excluindo Debuffer de propósito - ele já tem
        /// o kiting nativamente). Irrelevante para as outras Roles.</summary>
        public List<SupportType> SupportTypes { get; set; } = new();
    }
}
