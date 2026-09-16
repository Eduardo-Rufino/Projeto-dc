using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;
using System;
using System.Linq;

namespace ProjetoDC.Scripts.Systems.Passives
{
    /// <summary>
    /// Sorteio de passiva (ver PASSIVAS_SPEC.md na raiz do projeto, seção 1) - ao nascer ou
    /// evoluir, um Digimon sorteia exatamente 1 passiva do pool da própria espécie
    /// (DigimonData.Passives), restrita à Role (e ao SupportType, se for Support). Não decide
    /// NADA sobre o que a passiva faz em combate - isso vive em BattleUnit/DamageCalculator/
    /// BattleCombatant, cada um perto do próprio hook (ver seção 3 da spec).
    /// </summary>
    public static class PassiveSystem
    {
        private static readonly Random _random = new();

        /// <summary>
        /// Sorteia 1 passiva do pool de <paramref name="species"/>, filtrando contra o
        /// catálogo (Role/SupportType) por segurança mesmo que o pool já devesse estar
        /// coerente. <paramref name="species"/> deve ser a espécie "de ficha" vinda do banco
        /// (DatabaseManager.GetDigimon) - igual a Drops, o BaseData clonado de uma
        /// DigimonInstance não carrega o pool (ver DigimonInstance.CloneBaseData). Retorna
        /// null se o pool estiver vazio ou nenhuma entrada for compatível.
        /// </summary>
        public static PassiveType? RollRandomPassive(DigimonData species)
        {
            if (species == null || species.Passives == null || species.Passives.Count == 0)
                return null;

            var compatible = species.Passives
                .Where(id => IsCompatible(id, species))
                .ToList();

            if (compatible.Count == 0)
            {
                GD.PrintErr(
                    $"Pool de passivas de {species.Name} não tem nenhuma entrada compatível " +
                    $"com Role={species.Role}/SupportType={species.SupportType}."
                );

                return null;
            }

            return compatible[_random.Next(compatible.Count)];
        }

        /// <summary>Confere se uma passiva do catálogo (DatabaseManager) bate com a Role (e,
        /// se for Support, o SupportType) da espécie - mesma checagem que a spec pede em
        /// "Restrição de pool por Role" (seção 1).</summary>
        private static bool IsCompatible(PassiveType id, DigimonData species)
        {
            var data = DatabaseManager.Instance.GetPassive(id);

            if (data == null)
                return false;

            if (data.Role != species.Role)
                return false;

            if (species.Role == RoleType.Support &&
                data.SupportTypes.Count > 0 &&
                !data.SupportTypes.Contains(species.SupportType))
            {
                return false;
            }

            return true;
        }
    }
}
