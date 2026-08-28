using Godot;

namespace ProjetoDC.Scripts.World;

public partial class BroomWorld : Node2D
{
    public void SetPlacementMode(bool placementMode)
    {
        Modulate = placementMode
            ? new Color(1f, 1f, 1f, 0.6f)
            : new Color(1f, 1f, 1f, 1f);
    }
}
