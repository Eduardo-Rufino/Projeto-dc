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

        DatabaseManager.Instance.LoadDigimons();
        DatabaseManager.Instance.LoadEvolutions();

        CallDeferred(nameof(OpenTrainingCenter));
    }

    private void OpenTrainingCenter()
    {
        GetTree().ChangeSceneToFile("res://Scenes/TrainingCenter.tscn");
    }
	
}
