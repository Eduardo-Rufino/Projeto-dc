using Godot;
using ProjetoDC.Scripts.Gameplay;

namespace ProjetoDC.Scripts.World;

public partial class PoopWorld : Node2D
{
    private Poop _poop;

    public void Initialize(Poop poop)
    {
        _poop = poop;
    }

    public Poop GetPoop()
    {
        return _poop;
    }
}
