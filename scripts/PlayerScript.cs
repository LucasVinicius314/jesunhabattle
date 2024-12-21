using Godot;

public partial class PlayerScript : CharacterBody3D
{
  public const float Speed = 5.0f;
  public const float JumpVelocity = 4.5f;
  public const float MouseSensitivity = 0.1f;

  Camera3D? Camera;
  Vector2 MouseDelta;

  public override void _EnterTree()
  {
    SetMultiplayerAuthority(int.Parse(Name.ToString().Split("-")[1]));

    if (!IsMultiplayerAuthority())
    {
      return;
    }

    Camera = GetNode<Camera3D>("Camera3D");
    Camera?.MakeCurrent();
  }

  public override void _Input(InputEvent @event)
  {
    if (!IsMultiplayerAuthority())
    {
      return;
    }

    if (@event is InputEventMouseMotion mouseMotionEvent)
    {
      MouseDelta = mouseMotionEvent.Relative;
    }
    else if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.Pressed && mouseButtonEvent.ButtonIndex == MouseButton.Left)
    {
      Raycast();
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
      velocity.Y = JumpVelocity;
    }

    var inputDir = Input.GetVector("game_left", "game_right", "game_forward", "game_backward");
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

    RotateY(Mathf.DegToRad(-MouseDelta.X * MouseSensitivity));
    Camera?.RotateX(Mathf.DegToRad(-MouseDelta.Y * MouseSensitivity));

    if (Camera != null)
    {
      Camera.RotationDegrees = new Vector3(
        Mathf.Clamp(Camera.RotationDegrees.X, -90, 90),
        Camera.RotationDegrees.Y,
        Camera.RotationDegrees.Z
      );
    }

    MouseDelta = Vector2.Zero;

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
  }

  void Raycast()
  {
    if (Camera == null)
    {
      return;
    }

    var spaceState = GetWorld3D().DirectSpaceState;

    var query = PhysicsRayQueryParameters3D.Create(Camera.GlobalPosition, Camera.GlobalTransform.Origin + Camera.GlobalTransform.Basis.Z * -5, 1);

    var result = spaceState.IntersectRay(query);

    if (result.Count == 0)
    {
      return;
    }

    var collider = (Node3D)result["collider"];

    if (collider.HasMeta("unit") && collider.GetMeta("unit").AsBool())
    {
      var unitScript = collider as UnitScript;
      unitScript?.TakeDamage();
    }
  }
}
