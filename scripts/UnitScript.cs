using System;
using Godot;

public partial class UnitScript : CharacterBody3D
{
  public const float Speed = 5.0f;

  NavigationAgent3D Agent;
  Node3D Player;
  Timer Timer;

  public override void _PhysicsProcess(double delta)
  {
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

    if (Player != null)
    {
      var inputDir = GlobalPosition.DistanceTo(Player.GlobalPosition) > 3 ? Vector2.Up : Vector2.Zero;
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
      SetTarget();
    };
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
  void HandleClientTakeDamageRpc()
  {
    CallDeferred(Node.MethodName.QueueFree);
  }

  void SetTarget()
  {
    if (!Multiplayer.IsServer())
    {
      return;
    }

    Player = GetNode<Node3D>("/root/MainScene/Player-1");

    Agent.TargetPosition = Player.Position;
  }

  public void TakeDamage()
  {
    RpcId(1, MethodName.HandleClientTakeDamageRpc);
  }
}
