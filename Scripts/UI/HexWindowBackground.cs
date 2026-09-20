using Godot;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Fundo em hexágonos azuis (o mesmo motivo do menu principal) pras janelas do jogo. Vai como
    /// primeiro filho do painel da janela (nas cenas, um nó "HexBackground" com este script) e cobre
    /// o painel inteiro, com os cantos arredondados. O shader desenha em pixels, então precisa saber
    /// o tamanho do retângulo pra manter os hexágonos regulares.
    /// As janelas de confirmação montadas em código (<see cref="ConfirmWindow"/>) o adicionam por código.
    /// </summary>
    public partial class HexWindowBackground : ColorRect
    {
        private const string ShaderPath = "res://Scenes/UI/window_background.gdshader";

        private static Shader _sharedShader;

        // Raio dos cantos do painel de baixo (10 nas janelas do jogo).
        [Export] public float CornerRadius { get; set; } = 10f;

        private ShaderMaterial _material;

        public override void _Ready()
        {
            SetAnchorsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Ignore;

            _material = new ShaderMaterial { Shader = LoadShader() };
            _material.SetShaderParameter("corner_radius", CornerRadius);
            _material.SetShaderParameter("inset", 1f);
            Material = _material;

            Resized += UpdateSize;
            UpdateSize();
        }

        private void UpdateSize() => _material?.SetShaderParameter("rect_size", Size);

        private static Shader LoadShader() =>
            _sharedShader ??= GD.Load<Shader>(ShaderPath);
    }
}
