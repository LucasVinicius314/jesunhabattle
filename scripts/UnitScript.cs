using Godot;

public partial class UnitScript : CharacterBody3D
{
  [Export]
  float Speed = 5.0f;

  [Export]
  UnitType UnitType;

  NavigationAgent3D? Agent;
  Node3D? TargetNode;
  Timer? Timer;

  public override void _PhysicsProcess(double delta)
  {
    if (Agent == null)
    {
      return;
    }

    if (!Multiplayer.IsServer())
    {
      return;
    }

    var velocity = Velocity;

    if (!IsOnFloor())
    {
      velocity += GetGravity() * (float)delta;
    }

    var targetPosition = Agent.GetNextPathPosition();

    var distance = targetPosition.DistanceTo(GlobalPosition);

    if (TargetNode != null && IsInstanceValid(TargetNode))
    {
      var inputDir = GlobalPosition.DistanceTo(TargetNode.GlobalPosition) > 3 ? Vector2.Up : Vector2.Zero;
      var direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
      if (direction != Vector3.Zero)
      {
        velocity.X = direction.X * Speed;
        velocity.Z = direction.Z * Speed;
      }
      else
      {
        velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
        velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
      }

      if (distance > 0)
      {
        LookAt(targetPosition);
      }

      var rotation = Rotation;

      rotation.X = 0;
      rotation.Z = 0;

      Rotation = rotation;
    }
    else
    {
      velocity.X = 0;
      velocity.Z = 0;
    }

    Velocity = velocity;
    MoveAndSlide();
  }

  public override void _Ready()
  {
    if (!Multiplayer.IsServer())
    {
      return;
    }

    Agent = GetNode<NavigationAgent3D>("NavigationAgent3D");

    Timer = new Timer();

    AddChild(Timer);

    Timer.WaitTime = 1;
    Timer.OneShot = false;

    Timer.Timeout += () =>
    {
      SetTarget();
    };

    Timer.Start();

    Agent.NavigationFinished += () =>
    {
      GD.Print("navigation finished");

      switch (UnitType)
      {
        case UnitType.MELEE:
          break;
        case UnitType.MINER:
          if (TargetNode is OreScript oreScript)
          {
            oreScript.TakeDamage();
          }

          break;
      }

      SetTarget();
    };
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
  void ClientHandleTakeDamageRpc()
  {
    CallDeferred(Node.MethodName.QueueFree);
  }

  void SetTarget()
  {
    if (!Multiplayer.IsServer())
    {
      return;
    }

    switch (UnitType)
    {
      case UnitType.MELEE:
        TargetNode = GetNode<Node3D>("/root/MainScene/Player-1");
        break;
      case UnitType.MINER:
        Node3D? nearestNode = null;
        float nearestDistanceSquared = float.MaxValue;

        foreach (var node in GetTree().GetNodesInGroup("ores"))
        {
          if (node is Node3D node3D && node3D.HasMeta("ore") && (bool)node3D.GetMeta("ore"))
          {
            float distanceSquared = Position.DistanceSquaredTo(node3D.GlobalTransform.Origin);
            if (distanceSquared < nearestDistanceSquared)
            {
              nearestDistanceSquared = distanceSquared;
              nearestNode = node3D;
            }
          }
        }

        TargetNode = nearestNode;

        break;
    }

    if (Agent != null && TargetNode != null)
    {
      Agent.TargetPosition = TargetNode.Position;
    }
  }

  public void TakeDamage()
  {
    RpcId(1, MethodName.ClientHandleTakeDamageRpc);
  }
}
