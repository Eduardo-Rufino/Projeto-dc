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

    // Arrastar o ovo pelo Center, igual aos Digimons (ver DigimonWorld.StartDragging/StopDragging),
    // só que sem nenhum efeito de área: o ovo não tem status, então soltar no Treino/Dormitório/etc.
    // só muda onde ele fica. Soltar fora de uma área devolve pra onde estava.
    private static EggWorld _draggingInstance;
    private bool _isDragging;
    private Vector2 _dragOffset;
    private Vector2 _dragStartPosition;
    private CollisionShape2D _clickShape;

    /// <summary>Algum ovo está sendo arrastado agora (ver DigimonWorld.IsAnyDragging).</summary>
    public static bool IsAnyDragging => _draggingInstance != null;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite2D");

        _wobbleTimer = WobbleInterval;

        CreateClickArea();
    }

    public override void _ExitTree()
    {
        // Chocou (QueueFree) ou trocou de cena no meio do arraste: libera o lock.
        if (_draggingInstance == this)
            _draggingInstance = null;
    }

    // Criada em código pra cobrir o frame visível do sprite (a folha tem 3 frames lado a lado).
    private void CreateClickArea()
    {
        var clickArea = new Area2D { InputPickable = true };
        _clickShape = new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(48, 48) } };

        clickArea.AddChild(_clickShape);
        AddChild(clickArea);

        clickArea.InputEvent += OnClickAreaInputEvent;
    }

    private void UpdateClickAreaSize()
    {
        if (_sprite.Texture == null || _clickShape == null)
            return;

        var frameSize = new Vector2(_sprite.Texture.GetWidth() / (float)_sprite.Hframes, _sprite.Texture.GetHeight());
        _clickShape.Shape = new RectangleShape2D { Size = frameSize * _sprite.Scale };
    }

    private void OnClickAreaInputEvent(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            StartDragging();
    }

    private void StartDragging()
    {
        if (_egg == null || _isDragging)
            return;

        // Um clique só pega uma coisa: outro ovo ou um Digimon por baixo do cursor já foi pego.
        if (_draggingInstance != null || DigimonWorld.IsAnyDragging)
            return;

        _isDragging = true;
        _draggingInstance = this;
        _dragStartPosition = GlobalPosition;
        _dragOffset = GlobalPosition - GetGlobalMousePosition();

        // Por cima dos Digimons (z_index 1) enquanto é arrastado.
        ZIndex = 2;
    }

    private void StopDragging()
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        ZIndex = 0;

        if (_draggingInstance == this)
            _draggingInstance = null;

        var center = GetTree().CurrentScene as global::Center;

        if (center != null && center.GetAreaAtPosition(GlobalPosition) == null)
            GlobalPosition = _dragStartPosition;

        _egg.PositionX = GlobalPosition.X;
        _egg.PositionY = GlobalPosition.Y;
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isDragging)
            return;

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && !mouseEvent.Pressed)
            StopDragging();
    }

    public void Initialize(EggData egg)
    {
        _egg = egg;

        ApplyTexture();
        UpdateFrame();
        UpdateClickAreaSize();
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

        if (_isDragging)
            GlobalPosition = GetGlobalMousePosition() + _dragOffset;

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
