using Godot;
using ProjetoDC.Scripts.Gameplay;
using ProjetoDC.Scripts.Managers;
using ProjetoDC.Scripts.World;
using System.Collections.Generic;
using System.Linq;

public partial class Center : Node2D
{
    private PackedScene _digimonScene;

    private List<Marker2D> _spawnPoints = new();

    private List<Marker2D> _walkPoints = new();

    private readonly List<DigimonWorld> _digimonWorlds = new();

    private Node2D _digimonsContainer;

    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        PrintTree();

        _digimonScene = GD.Load<PackedScene>(
            "res://Scenes/Center/DigimonWorld.tscn"
        );

        if (_digimonScene == null)
        {
            GD.PrintErr("Não foi possível carregar DigimonWorld.tscn");
            return;
        }

        var spawnRoot = GetNode<Node2D>("Digimons/SpawnPoints");

        foreach (Node child in spawnRoot.GetChildren())
        {
            if (child is Marker2D marker)
                _spawnPoints.Add(marker);
        }

        var walkRoot = GetNode<Node2D>("Digimons/WalkPoints");

        foreach (Node child in walkRoot.GetChildren())
        {
            if (child is Marker2D marker)
            {
                _walkPoints.Add(marker);
                GD.Print($"WalkPoint adicionado: {marker.Name} - {marker.GlobalPosition}");
            }

        }

        _digimonsContainer = GetNode<Node2D>("Digimons");

        SpawnDigimons();
    }

    private void SpawnDigimons()
    {
        var digimons = GameManager.Instance.CenterService.GetAllDigimons();

        for (int i = 0; i < digimons.Count; i++)
        {
            if (i >= _spawnPoints.Count)
            {
                GD.PrintErr("Não há SpawnPoints suficientes.");
                break;
            }

            SpawnDigimon(digimons[i], _spawnPoints[i]);
        }
    }

    private DigimonWorld SpawnDigimon(
        DigimonInstance digimon,
        Marker2D spawnPoint)
    {
        GD.Print($"Criando DigimonWorld: {digimon.BaseData.Name}");

        var instance = _digimonScene.Instantiate<DigimonWorld>();

        _digimonsContainer.AddChild(instance);

        instance.SetCenter(this);

        instance.GlobalPosition = spawnPoint.GlobalPosition;

        instance.Initialize(digimon);

        GD.Print($"Spawn em {instance.GlobalPosition}");

        _digimonWorlds.Add(instance);

        return instance;
    }

    public Vector2 GetRandomWalkPoint()
    {
        if (_walkPoints.Count == 0)
            return Vector2.Zero;

        int index = _rng.RandiRange(0, _walkPoints.Count - 1);

        return _walkPoints[index].GlobalPosition;
    }
}