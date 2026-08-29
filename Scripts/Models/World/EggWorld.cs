using Godot;
using ProjetoDC.Scripts.Data;

namespace ProjetoDC.Scripts.World;

public partial class EggWorld : Node2D
{
    private const int IdleFrameA = 0;
    private const int IdleFrameB = 1;
    private const int CrackFrame = 2;
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

        UpdateFrame();
    }

    public EggData GetEgg()
    {
        return _egg;
    }

    public override void _Process(double delta)
    {
        if (_egg == null)
            return;

        if (_egg.IsReady)
        {
            UpdateFrame();
            return;
        }

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
        _sprite.Frame = _egg.IsReady
            ? CrackFrame
            : (_showingFrameB ? IdleFrameB : IdleFrameA);
    }
}
