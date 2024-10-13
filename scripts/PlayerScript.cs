using Godot;

public partial class PlayerScript : CharacterBody3D
{
  public const float Speed = 5.0f;
  public const float JumpVelocity = 4.5f;
  public const float MouseSensitivity = 0.1f;

  private Camera3D _camera;
  private Vector2 _mouseDelta;

  public override void _Ready()
  {
    _camera = GetNode<Camera3D>("Camera3D");
    Input.MouseMode = Input.MouseModeEnum.Captured;
  }

  public override void _Input(InputEvent @event)
  {
    if (@event is InputEventMouseMotion mouseEvent)
    {
      _mouseDelta = mouseEvent.Relative;
    }
    else if (@event is InputEventKey eventKey && eventKey.Pressed)
    {
      if (Input.IsActionPressed("ui_cancel"))
      {
        Input.MouseMode = Input.MouseModeEnum.Visible;
      }
    }
  }

  public override void _PhysicsProcess(double delta)
  {
    Vector3 velocity = Velocity;

    if (!IsOnFloor())
    {
      velocity += GetGravity() * (float)delta;
    }

    if (Input.IsActionJustPressed("game_jump") && IsOnFloor())
    {
      velocity.Y = JumpVelocity;
    }

    Vector2 inputDir = Input.GetVector("game_left", "game_right", "game_forward", "game_backward");
    Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
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

    RotateY(Mathf.DegToRad(-_mouseDelta.X * MouseSensitivity));
    _camera.RotateX(Mathf.DegToRad(-_mouseDelta.Y * MouseSensitivity));

    _camera.RotationDegrees = new Vector3(
        Mathf.Clamp(_camera.RotationDegrees.X, -90, 90),
        _camera.RotationDegrees.Y,
        _camera.RotationDegrees.Z
    );

    _mouseDelta = Vector2.Zero;

    Velocity = velocity;
    MoveAndSlide();
  }
}
