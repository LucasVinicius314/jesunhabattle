using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public partial class PlayerScript : CharacterBody3D
{
  readonly PackedScene SpawnerPrefab = GD.Load<PackedScene>("res://prefabs/spawner.tscn");

  public const float jumpVelocity = 4.5f;
  public const float mouseSensitivity = 0.1f;
  public const float speed = 5.0f;

  Camera3D? camera;
  public string? factionId;
  MainSceneScript? mainSceneScript;
  Vector2 mouseDelta;
  PlayerUiScript? playerUiScript;

  public override void _EnterTree()
  {
    SetMultiplayerAuthority(int.Parse(Name.ToString().Split("-")[1]));

    if (!IsMultiplayerAuthority())
    {
      return;
    }

    camera = GetNode<Camera3D>("Camera3D");
    camera?.MakeCurrent();

    playerUiScript = camera?.GetNode<PlayerUiScript>("PlayerUi");
  }

  public override void _Input(InputEvent @event)
  {
    if (!IsMultiplayerAuthority())
    {
      return;
    }

    if (@event is InputEventMouseMotion mouseMotionEvent)
    {
      mouseDelta = mouseMotionEvent.Relative;
    }
    else if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.Pressed)
    {
      switch (mouseButtonEvent.ButtonIndex)
      {
        case MouseButton.Left:
          {
            var result = Raycast(1);
            if (result != null)
            {
              if (result.Value.Item1 is OreScript oreScript)
              {
                oreScript?.TakeDamage();
              }
              else if (result.Value.Item1 is UnitScript unitScript)
              {
                unitScript?.TakeDamage();
              }
            }

            break;
          }
        case MouseButton.Right:
          {
            var result = Raycast(1);
            if (result != null && result.Value.Item1.Name == "Ground")
            {
              RpcId(1, MethodName.ServerHandleAddSpawnerRpc, result.Value.Item2 + new Vector3(0, 1, 0));
            }

            break;
          }
      }
    }
    else if (@event is InputEventKey eventKey && eventKey.Pressed && Input.IsActionPressed("ui_cancel"))
    {
      Input.MouseMode = Input.MouseModeEnum.Visible;
    }
  }

  public override void _PhysicsProcess(double delta)
  {
    if (!IsMultiplayerAuthority())
    {
      return;
    }

    var velocity = Velocity;

    if (!IsOnFloor())
    {
      velocity += GetGravity() * (float)delta;
    }

    if (Input.IsActionJustPressed("game_jump") && IsOnFloor())
    {
      velocity.Y = jumpVelocity;
    }

    var inputDir = Input.GetVector("game_left", "game_right", "game_forward", "game_backward");
    var direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
    if (direction != Vector3.Zero)
    {
      velocity.X = direction.X * speed;
      velocity.Z = direction.Z * speed;
    }
    else
    {
      velocity.X = Mathf.MoveToward(Velocity.X, 0, speed);
      velocity.Z = Mathf.MoveToward(Velocity.Z, 0, speed);
    }

    RotateY(Mathf.DegToRad(-mouseDelta.X * mouseSensitivity));
    camera?.RotateX(Mathf.DegToRad(-mouseDelta.Y * mouseSensitivity));

    if (camera != null)
    {
      camera.RotationDegrees = new Vector3(
        Mathf.Clamp(camera.RotationDegrees.X, -90, 90),
        camera.RotationDegrees.Y,
        camera.RotationDegrees.Z
      );
    }

    mouseDelta = Vector2.Zero;

    Velocity = velocity;
    MoveAndSlide();
  }

  public override void _Ready()
  {
    if (!IsMultiplayerAuthority())
    {
      return;
    }

    Input.MouseMode = Input.MouseModeEnum.Captured;

    mainSceneScript = GetNode<Node3D>("/root/MainScene") as MainSceneScript;

    RetrieveGameState();
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
  public void ClientHandleReceiveGameStateRpc(string factionMapPayload)
  {
    var factionMap = JsonSerializer.Deserialize<Dictionary<string, Faction>>(factionMapPayload);
    if (!factionMap.ContainsKey(factionId))
    {
      return;
    }

    var faction = factionMap[factionId];

    if (playerUiScript != null)
    {
      playerUiScript.moneyLabel.Text = $"Money: {faction.money}";
    }
  }

  (Node3D, Vector3)? Raycast(uint collisionMask)
  {
    if (camera == null)
    {
      return null;
    }

    var query = PhysicsRayQueryParameters3D.Create(camera.GlobalPosition, camera.GlobalTransform.Origin + camera.GlobalTransform.Basis.Z * -5, collisionMask);

    var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
    if (result.Count == 0)
    {
      return null;
    }

    return ((Node3D)result["collider"], (Vector3)result["position"]);
  }

  void RetrieveGameState()
  {
    RpcId(1, MethodName.ServerHandleRetrieveGameState);
  }

  public void SendGameStateToClient(long id)
  {
    if (mainSceneScript == null)
    {
      return;
    }

    RpcId(id, MethodName.ClientHandleReceiveGameStateRpc, JsonSerializer.Serialize(mainSceneScript.factionMap));
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
  public void ServerHandleAddSpawnerRpc(Vector3 position)
  {
    var spawner = SpawnerPrefab.Instantiate<Node3D>();
    spawner.GlobalTransform = new Transform3D(Basis.Identity, position);

    MainSceneScript.navigationRegion?.CallDeferred(Node.MethodName.AddChild, spawner);

    MainSceneScript.navigationRegion?.CallDeferred(NavigationRegion3D.MethodName.BakeNavigationMesh);
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
  public void ServerHandleRetrieveGameState()
  {
    SendGameStateToClient(Multiplayer.GetRemoteSenderId());
  }
}
