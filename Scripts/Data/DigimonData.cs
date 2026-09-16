using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjetoDC.Enums;

namespace ProjetoDC.Scripts.Data
{
    /// <summary>
    /// Classe que representa os dados estáticos de um Digimon.
    /// Os dados são carregados a partir de um arquivo JSON que serve como base de dados.
    /// </summary>
    public class DigimonData
    {
        /// <summary>
        /// Obtém ou define o identificador único do Digimon.
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Obtém ou define o código do Digimon.
        /// </summary>
        public string Code { get; set; }
        
        /// <summary>
        /// Obtém ou define o nome do Digimon.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Obtém ou define o estágio do Digimon.
        /// </summary>
        public DigimonStage Stage { get; set; }
        
        /// <summary>
        /// Obtém ou define o papel do Digimon.
        /// </summary>
        public RoleType Role { get; set; }
        
        /// <summary>
        /// Obtém ou define o atributo do Digimon.
        /// </summary>
        public DigimonAttribute Attribute { get; set; }
        
        /// <summary>
        /// Obtém ou define o elemento do Digimon.
        /// </summary>
        public DigimonElement Element { get; set; }
        
        /// <summary>
        /// Obtém ou define o tipo de ovo do Digimon.
        /// </summary>
        public EggType EggType { get; set; }

        public AttackType AttackType { get; set; }

        /// <summary>
        /// Especialização do Digimon quando Role == Support (Healer/Buffer/Debuffer).
        /// Irrelevante para as outras Roles.
        /// </summary>
        public SupportType SupportType { get; set; }
        
        /// <summary>
        /// Obtém ou define os stats base do Digimon.
        /// </summary>
        public BaseStats BaseStats { get; set; } = new();

        /// <summary>Itens que essa espécie pode soltar ao ser derrotada num encontro selvagem
        /// de exploração (ver GameManager.ApplyWildEncounterDrops) - vazio por padrão, só
        /// precisa ser preenchido nas espécies que realmente tenham drop.</summary>
        public List<DigimonDropData> Drops { get; set; } = new();

        /// <summary>Pool de passivas possíveis dessa espécie (2 ou 3, ver PASSIVAS_SPEC.md na
        /// raiz do projeto, seção 1) - ao nascer ou evoluir, a instância sorteia exatamente 1
        /// passiva desse pool (ver PassiveSystem.RollRandomPassive) e ela não muda depois até
        /// a próxima evolução. Vazio por padrão até a espécie ser preenchida - igual a Drops,
        /// a leitura deve ser feita na espécie "de ficha" via DatabaseManager.GetDigimon, não
        /// no BaseData clonado de uma DigimonInstance (ver DigimonInstance.CloneBaseData, que
        /// não copia esse campo pelo mesmo motivo).</summary>
        public List<PassiveType> Passives { get; set; } = new();
    }
}
