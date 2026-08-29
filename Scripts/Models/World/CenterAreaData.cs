using ProjetoDC.Enums;

namespace ProjetoDC.Scripts.Models.World
{
    /// <summary>
    /// Registro salvo de uma área do Center construída pelo jogador (posição no grid +
    /// tipo), pra poder recriar a área visual quando o save é carregado de novo. A área
    /// inicial (0,0) não entra aqui - ela já vem fixa em Center.tscn.
    /// </summary>
    public class CenterAreaData
    {
        public int GridX { get; set; }

        public int GridY { get; set; }

        public CenterAreaType AreaType { get; set; }
    }
}
