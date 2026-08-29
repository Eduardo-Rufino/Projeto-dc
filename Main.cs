using Godot;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.Models.World;
using ProjetoDC.Scripts.Systems;

public partial class Main : Node
{
    public override void _Ready()
    {
        var gm = GameManager.Instance;

        //GameManager.Instance.NewGame();

        CallDeferred(nameof(OpenCenterScreen));
    }

    private void OpenCenterScreen()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Center/CenterScreen.tscn");
    }
	
}
