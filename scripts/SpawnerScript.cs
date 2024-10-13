using Godot;

public partial class SpawnerScript : Node3D
{
  [Export]
  public PackedScene ObjectToSpawn;

  [Export]
  public float SpawnInterval = 10.0f;

  private Timer _timer;

  public override void _Ready()
  {
    ObjectToSpawn = ResourceLoader.Load<PackedScene>("res://prefabs/unit.tscn");

    _timer = new Timer();

    AddChild(_timer);

    _timer.WaitTime = SpawnInterval;
    _timer.OneShot = false;

    _timer.Timeout += () =>
    {
      if (ObjectToSpawn != null)
      {
        var instance = ObjectToSpawn.Instantiate<Node3D>();
        AddChild(instance);
        instance.GlobalTransform = new Transform3D(Basis.Identity, GlobalTransform.Origin + GlobalTransform.Basis.Z * -1);
      }
    };

    _timer.Start();
  }
}
