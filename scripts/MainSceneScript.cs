using Godot;

public partial class MainSceneScript : Node3D
{
  readonly PackedScene PlayerPrefab = GD.Load<PackedScene>("res://prefabs/player.tscn");
  readonly PackedScene SpawnerPrefab = GD.Load<PackedScene>("res://prefabs/spawner.tscn");

  ENetMultiplayerPeer Peer = new ENetMultiplayerPeer();

  public override void _Input(InputEvent @event)
  {
    if (@event is InputEventKey eventKey && eventKey.Pressed)
    {
      switch (eventKey.Keycode)
      {
        case Key.H:
          Host();
          break;
        case Key.J:
          Join();
          break;
      }
    }
  }

  void AddPlayer(long id)
  {
    var player = PlayerPrefab.Instantiate<CharacterBody3D>();
    player.GlobalTransform = new Transform3D(Basis.Identity, Vector3.Zero);
    player.Name = $"Player-{id}";

    CallDeferred(Node.MethodName.AddChild, player);
  }

  void Host()
  {
    Peer.CreateServer(5001);
    Multiplayer.MultiplayerPeer = Peer;

    Multiplayer.PeerConnected += (long id) =>
    {
      AddPlayer(id);
    };

    AddPlayer(1);

    var spawner = SpawnerPrefab.Instantiate<Node3D>();
    spawner.GlobalTransform = new Transform3D(Basis.Identity, new Vector3(0, 1, -7));

    CallDeferred(Node.MethodName.AddChild, spawner);
  }

  void Join()
  {
    Peer.CreateClient("localhost", 5001);
    Multiplayer.MultiplayerPeer = Peer;
  }
}
