using ProjetoDC.Enums;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>Caminhos dos ícones de Atributo/Elemento (Assets/Sprites/icons/) - usado por
    /// qualquer tela que precise mostrar visualmente o tipo de um Digimon.</summary>
    public static class TypeIcons
    {
        private const string Folder = "res://Assets/Sprites/icons/";

        public static string GetPath(DigimonAttribute attribute) => attribute switch
        {
            DigimonAttribute.Vacine => Folder + "vaccine.png",
            DigimonAttribute.Virus => Folder + "virus.png",
            DigimonAttribute.Data => Folder + "data.png",
            DigimonAttribute.Free => Folder + "no.png",
            DigimonAttribute.Unknown => Folder + "unknown.png",
            _ => Folder + "unknown.png"
        };

        public static string GetPath(DigimonElement element) => element switch
        {
            DigimonElement.Fire => Folder + "fogo.png",
            DigimonElement.Water => Folder + "agua.png",
            DigimonElement.Plant => Folder + "planta.png",
            DigimonElement.Electric => Folder + "trovao.png",
            DigimonElement.Wind => Folder + "vento.png",
            DigimonElement.Earth => Folder + "terra.png",
            DigimonElement.Light => Folder + "luz.png",
            DigimonElement.Dark => Folder + "trevas.png",
            DigimonElement.Neutral => Folder + "neutro.png",
            _ => Folder + "neutro.png"
        };
    }
}
