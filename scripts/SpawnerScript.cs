using Godot;

public partial class SpawnerScript : Node3D
{
  [Export]
  public PackedScene ObjectToSpawn;

  [Export]
  public float SpawnInterval = 10.0f;

  Timer? Timer;

  public override void _EnterTree()
  {
    if (!Multiplayer.IsServer())
    {
      return;
    }

    Timer?.Stop();
  }

  public override void _ExitTree()
  {
    if (!Multiplayer.IsServer())
    {
      return;
    }

    Timer?.Stop();
  }

  public override void _Ready()
  {
    if (!Multiplayer.IsServer())
    {
      return;
    }

    ObjectToSpawn = ResourceLoader.Load<PackedScene>("res://prefabs/unit.tscn");

    Timer = new Timer();

    AddChild(Timer);

    Timer.WaitTime = SpawnInterval;
    Timer.OneShot = false;

    Timer.Timeout += () =>
    {
      Spawn();
    };

    Timer.Start();

    Spawn();
  }

  void Spawn()
  {
    if (ObjectToSpawn != null)
    {
      GD.Print($"{Multiplayer.GetUniqueId()} spawning");

      var instance = ObjectToSpawn.Instantiate<Node3D>();
      instance.GlobalTransform = new Transform3D(Basis.Identity, GlobalTransform.Origin + GlobalTransform.Basis.Z * -5);
      instance.Name = "Unit";

      GetTree().Root.GetChild(0).CallDeferred(Node.MethodName.AddChild, instance, true);
    }
  }
}
