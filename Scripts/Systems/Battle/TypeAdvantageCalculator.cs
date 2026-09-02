using ProjetoDC.Enums;
using ProjetoDC.Scripts.Data;

namespace ProjetoDC.Scripts.Systems.Battle
{
    /// <summary>
    /// Sistema de vantagem/desvantagem de tipo (Atributo + Elemento), combinado num único
    /// multiplicador de dano. Cada sistema conta como +1 (vantagem) ou -1 (desvantagem) pro
    /// "placar" de tipo; os dois se somam antes de virar multiplicador, então uma vantagem
    /// numa e desvantagem na outra se cancelam (líquido 0 = neutro).
    ///
    /// Atributo (triângulo clássico Vaccine/Virus/Data, mais os extremos Unknown/Free):
    /// Vacine > Virus > Data > Vacine. Unknown vence todos exceto Free (contra quem é
    /// neutro). Free perde pra todos exceto Unknown (contra quem também é neutro).
    ///
    /// Elemento (mesmo sistema dos jogos "Story", ex.: Cyber Sleuth - não existe uma roda
    /// elemental única "oficial" pra franquia inteira, essa é a mais consistente que existe):
    /// Fire > Plant > Water > Fire. Electric > Wind > Earth > Electric. Light e Dark são
    /// mútuos (cada um vence o outro). Neutral não participa (nem dá nem recebe vantagem).
    /// </summary>
    public static class TypeAdvantageCalculator
    {
        public static float GetDamageMultiplier(DigimonData attacker, DigimonData defender)
        {
            int score = 0;

            score += GetPairScore(HasAttributeAdvantage(attacker.Attribute, defender.Attribute),
                HasAttributeAdvantage(defender.Attribute, attacker.Attribute));

            score += GetPairScore(HasElementAdvantage(attacker.Element, defender.Element),
                HasElementAdvantage(defender.Element, attacker.Element));

            return score switch
            {
                >= 2 => 2.0f,
                1 => 1.3f,
                0 => 1.0f,
                -1 => 0.7f,
                _ => 0.5f, // <= -2
            };
        }

        private static int GetPairScore(bool attackerHasAdvantage, bool defenderHasAdvantage)
        {
            if (attackerHasAdvantage)
                return 1;

            if (defenderHasAdvantage)
                return -1;

            return 0;
        }

        private static bool HasAttributeAdvantage(DigimonAttribute attacker, DigimonAttribute defender)
        {
            // Triângulo: Vacine > Virus > Data > Vacine.
            if (attacker == DigimonAttribute.Vacine && defender == DigimonAttribute.Virus)
                return true;

            if (attacker == DigimonAttribute.Virus && defender == DigimonAttribute.Data)
                return true;

            if (attacker == DigimonAttribute.Data && defender == DigimonAttribute.Vacine)
                return true;

            // Unknown vence todos, exceto Free (com quem é neutro).
            if (attacker == DigimonAttribute.Unknown && defender != DigimonAttribute.Free)
                return true;

            // Free perde pra todos, exceto Unknown (com quem é neutro) - ou seja, qualquer
            // atacante que não seja Unknown (nem o próprio Free) tem vantagem contra Free.
            if (defender == DigimonAttribute.Free &&
                attacker != DigimonAttribute.Unknown &&
                attacker != DigimonAttribute.Free)
                return true;

            return false;
        }

        private static bool HasElementAdvantage(DigimonElement attacker, DigimonElement defender)
        {
            // Triângulo 1: Fire > Plant > Water > Fire.
            if (attacker == DigimonElement.Fire && defender == DigimonElement.Plant)
                return true;

            if (attacker == DigimonElement.Plant && defender == DigimonElement.Water)
                return true;

            if (attacker == DigimonElement.Water && defender == DigimonElement.Fire)
                return true;

            // Triângulo 2: Electric > Wind > Earth > Electric.
            if (attacker == DigimonElement.Electric && defender == DigimonElement.Wind)
                return true;

            if (attacker == DigimonElement.Wind && defender == DigimonElement.Earth)
                return true;

            if (attacker == DigimonElement.Earth && defender == DigimonElement.Electric)
                return true;

            // Light e Dark são mútuos - cada um vence o outro.
            if (attacker == DigimonElement.Light && defender == DigimonElement.Dark)
                return true;

            if (attacker == DigimonElement.Dark && defender == DigimonElement.Light)
                return true;

            // Neutral não entra na roda - nunca dá vantagem.
            return false;
        }
    }
}
