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
        /// Obtém ou define os stats base do Digimon.
        /// </summary>
        public BaseStats BaseStats { get; set; } = new();
    }
}
