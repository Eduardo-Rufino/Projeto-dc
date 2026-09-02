using Godot;
using ProjetoDC.Enums;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.World;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoDC.Scripts.UI
{
    /// <summary>
    /// Esquema simplificado do Center (posição/tipo de cada CenterArea, escalado pra caber
    /// na caixinha da barra superior) - não é uma segunda câmera renderizando o mundo de
    /// verdade, é só um desenho vetorial (mesmo espírito do AreaTypeIcon): mostra as áreas
    /// e um ícone identificando cada uma, sem Digimons/comida/coco. Áreas nunca são removidas
    /// nesse jogo, só adicionadas, então RefreshAreas só reconstrói quando a contagem muda.
    /// </summary>
    public partial class MiniMap : Control
    {
        private const float Padding = 10f;
        private const float AreaRadius = 9f;
        private const float MaxScale = 1f;
        private const float MiniIconSize = 13f;
        private const float MiniIconLineWidth = 1.6f;

        private static readonly Color BorderColor = new(0f, 0f, 0f, 0.6f);

        private List<(Vector2I GridPosition, CenterAreaType Type)> _areas = new();
        private readonly List<AreaTypeIcon> _icons = new();

        public override void _Ready()
        {
            Resized += QueueRedraw;
        }

        public void RefreshAreas(IEnumerable<CenterArea> areas)
        {
            var snapshot = areas
                .Select(a => (a.GridPosition, a.AreaType))
                .ToList();

            if (snapshot.Count == _areas.Count)
                return;

            _areas = snapshot;

            foreach (var icon in _icons)
                icon.QueueFree();

            _icons.Clear();

            foreach (var (_, type) in _areas)
            {
                var icon = new AreaTypeIcon
                {
                    IconSize = MiniIconSize,
                    LineWidth = MiniIconLineWidth,
                    MouseFilter = MouseFilterEnum.Ignore
                };

                AddChild(icon);

                icon.Setup(type, CenterArea.GetAreaColor(type));

                _icons.Add(icon);
            }

            QueueRedraw();
        }

        public override void _Draw()
        {
            if (_areas.Count == 0)
                return;

            var worldPositions = _areas
                .Select(a => CenterGrid.GridToWorld(a.GridPosition))
                .ToList();

            float minX = worldPositions.Min(p => p.X);
            float maxX = worldPositions.Max(p => p.X);
            float minY = worldPositions.Min(p => p.Y);
            float maxY = worldPositions.Max(p => p.Y);

            float worldWidth = Mathf.Max(maxX - minX, 1f);
            float worldHeight = Mathf.Max(maxY - minY, 1f);

            Vector2 available = Size - new Vector2(Padding * 2f, Padding * 2f);

            float scale = Mathf.Min(available.X / worldWidth, available.Y / worldHeight);
            scale = Mathf.Min(scale, MaxScale);

            Vector2 worldCenter = new((minX + maxX) / 2f, (minY + maxY) / 2f);
            Vector2 mapCenter = Size / 2f;

            for (int i = 0; i < _areas.Count; i++)
            {
                Vector2 localPos = mapCenter + (worldPositions[i] - worldCenter) * scale;
                Color color = CenterArea.GetAreaColor(_areas[i].Type);

                DrawCircle(localPos, AreaRadius, color);
                DrawArc(localPos, AreaRadius, 0f, Mathf.Tau, 20, BorderColor, 1.5f, true);

                if (i < _icons.Count)
                    _icons[i].Position = localPos;
            }
        }
    }
}
