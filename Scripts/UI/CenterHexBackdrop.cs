using Godot;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.World;
using System.Collections.Generic;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Sombra e base de pedra de cada sala do Center, desenhadas no mundo (acompanham a câmera) e na
    /// mesma escala/posição das salas, em qualquer quantidade ou arranjo: sincroniza com os filhos de
    /// <see cref="AreasPath"/> (CenterAreas no Center, CombatAreas na arena de batalha) a cada frame, então áreas construídas depois aparecem sozinhas. Também aplica o
    /// tom noturno leve nas salas/bases (RoomTint da tabela em <see cref="CenterSkyConfig"/>).
    /// <list type="bullet">
    /// <item>Sombras: todas dentro de um único CanvasGroup com alpha 0.3, pra sombras vizinhas não
    /// escurecerem em dobro onde se sobrepõem. Ficam abaixo de tudo do Center (z -11).</item>
    /// <item>Bases: z -6, entre o preenchimento das salas (z -10) e a ilustração (z -5) - a parte de
    /// cima fica escondida sob a sala e só a espessura das arestas de baixo aparece. Como toda
    /// ilustração de sala fica acima de toda base, uma sala mais abaixo sempre cobre a base da de
    /// cima, que é a mesma ordem de profundidade que a ordem por y das salas daria.</item>
    /// </list>
    /// </summary>
    public partial class CenterHexBackdrop : Node2D
    {
        private sealed class Pieces
        {
            public Sprite2D Shadow;
            public Sprite2D Base;
        }

        /// <summary>Nó pai das salas, relativo a este nó. No Center é "CenterAreas"; a arena de batalha
        /// aponta pra "CombatAreas" (ver BattleArena.tscn). Só os filhos CenterArea entram.</summary>
        [Export] public NodePath AreasPath { get; set; } = new NodePath("../CenterAreas");

        private readonly Dictionary<CenterArea, Pieces> _pieces = new();
        private readonly List<CenterArea> _stale = new();

        private CanvasGroup _shadowGroup;
        private Node2D _baseLayer;
        private Node2D _areasRoot;

        private Texture2D _shadowTexture;
        private Texture2D _baseTexture;

        public override void _Ready()
        {
            _areasRoot = GetNodeOrNull<Node2D>(AreasPath);

            if (_areasRoot == null)
                GD.PrintErr($"CenterHexBackdrop: nó '{AreasPath}' não encontrado - sem sombras/bases.");

            _shadowTexture = GD.Load<Texture2D>(CenterSkyConfig.TextureFolder + CenterSkyConfig.ShadowTexture);
            _baseTexture = GD.Load<Texture2D>(CenterSkyConfig.TextureFolder + CenterSkyConfig.BaseTexture);

            _shadowGroup = new CanvasGroup
            {
                Name = "Shadows",
                ZIndex = CenterSkyConfig.ShadowZIndex,
                SelfModulate = new Color(1f, 1f, 1f, CenterSkyConfig.ShadowAlpha),
                TextureFilter = TextureFilterEnum.Nearest,
            };

            _baseLayer = new Node2D
            {
                Name = "Bases",
                ZIndex = CenterSkyConfig.BaseZIndex,
                TextureFilter = TextureFilterEnum.Nearest,
            };

            AddChild(_shadowGroup);
            AddChild(_baseLayer);
        }

        public override void _Process(double delta)
        {
            if (_areasRoot == null)
                return;

            SyncPieces();
            ApplyRoomTint();
        }

        private void SyncPieces()
        {
            _stale.Clear();

            foreach (var area in _pieces.Keys)
            {
                if (!IsInstanceValid(area) || !area.IsInsideTree())
                    _stale.Add(area);
            }

            foreach (var area in _stale)
                RemovePieces(area);

            for (int i = 0; i < _areasRoot.GetChildCount(); i++)
            {
                if (_areasRoot.GetChild(i) is not CenterArea area)
                    continue;

                if (!_pieces.TryGetValue(area, out var pieces))
                {
                    pieces = CreatePieces();
                    _pieces[area] = pieces;
                }

                PlacePieces(area, pieces);
            }
        }

        private Pieces CreatePieces()
        {
            var pieces = new Pieces
            {
                Shadow = new Sprite2D { Centered = false, Texture = _shadowTexture, TextureFilter = TextureFilterEnum.Nearest },
                Base = new Sprite2D { Centered = false, Texture = _baseTexture, TextureFilter = TextureFilterEnum.Nearest },
            };

            _shadowGroup.AddChild(pieces.Shadow);
            _baseLayer.AddChild(pieces.Base);

            return pieces;
        }

        private void RemovePieces(CenterArea area)
        {
            if (_pieces.Remove(area, out var pieces))
            {
                pieces.Shadow.QueueFree();
                pieces.Base.QueueFree();
            }
        }

        // A sala é um sprite de 252x218 esticado até a caixa do hexágono (2R x R*raiz de 3), centrado
        // na origem da CenterArea - o canto superior esquerdo da sala é, portanto, -caixa/2. Sombra e
        // base usam o mesmo canto e a mesma escala; o deslocamento da sombra vem em pixels de arte.
        private static void PlacePieces(CenterArea area, Pieces pieces)
        {
            Vector2 box = new(CenterGrid.HexagonRadius * 2f, CenterGrid.HexagonRadius * Mathf.Sqrt(3f));
            Vector2 roomScale = box / CenterSkyConfig.RoomArtSize;
            Vector2 topLeft = -box / 2f;

            Vector2 areaScale = area.GlobalScale;

            pieces.Base.GlobalPosition = area.ToGlobal(topLeft);
            pieces.Base.Scale = roomScale * areaScale;

            pieces.Shadow.GlobalPosition = area.ToGlobal(topLeft + CenterSkyConfig.ShadowOffsetArt * roomScale);
            pieces.Shadow.Scale = roomScale * areaScale;
        }

        // Tom noturno leve nas salas (o pai CenterAreas) e nas bases; os Digimons, comidas e ovos ficam
        // fora (não são filhos de CenterAreas), então continuam com a cor normal.
        private void ApplyRoomTint()
        {
            Color tint = Colors.White;

            if (CenterSkyConfig.RoomNightTintEnabled)
                tint = CenterSkyConfig.Sample(CenterSkyClock.CurrentHour).RoomTint;

            _areasRoot.Modulate = tint;
            _baseLayer.Modulate = tint;
        }
    }
}
