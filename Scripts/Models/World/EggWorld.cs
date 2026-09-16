using Godot;
using ProjetoDC.Scripts.Data;
using ProjetoDC.Scripts.Managers;

namespace ProjetoDC.Scripts.World;

public partial class EggWorld : Node2D
{
    // Só os frames 0/1 (o "chacoalhar" do ovo) são usados - a sprite sheet tem um 3º frame
    // (rachadura) que nunca é mostrado: o ovo vira Digimon quase no mesmo instante em que
    // IncubationProgress bate IncubationTime (ver EggSystem.AdvanceHour/AdvanceDay -
    // TryHatchEgg roda no mesmo tick que IsReady vira true), então esse frame nunca ficava
    // visível tempo suficiente pra valer a pena mesmo antes disso.
    private const int IdleFrameA = 0;
    private const int IdleFrameB = 1;
    private const double WobbleInterval = 0.6;

    private Sprite2D _sprite;
    private EggData _egg;
    private double _wobbleTimer;
    private bool _showingFrameB;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");

        _wobbleTimer = WobbleInterval;
    }

    public void Initialize(EggData egg)
    {
        _egg = egg;

        ApplyTexture();
        UpdateFrame();
    }

    /// <summary>Cada espécie de Digimon Baby já vem com seu EggType de ficha (ver
    /// DigimonData.EggType/EggVisuals) - o sprite do ovo reflete exatamente o que vai nascer
    /// dele, sem nenhum mistério nem sprite genérico.</summary>
    private void ApplyTexture()
    {
        var digimonData = DatabaseManager.Instance?.GetDigimon(_egg.BaseDigimonId);

        if (digimonData == null)
            return;

        string path = EggVisuals.GetTexturePath(digimonData.EggType);

        if (ResourceLoader.Exists(path))
            _sprite.Texture = GD.Load<Texture2D>(path);
    }

    public EggData GetEgg()
    {
        return _egg;
    }

    public override void _Process(double delta)
    {
        if (_egg == null)
            return;

        _wobbleTimer -= delta;

        if (_wobbleTimer <= 0)
        {
            _wobbleTimer = WobbleInterval;
            _showingFrameB = !_showingFrameB;

            UpdateFrame();
        }
    }

    private void UpdateFrame()
    {
        _sprite.Frame = _showingFrameB ? IdleFrameB : IdleFrameA;
    }
}
