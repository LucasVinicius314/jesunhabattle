using System;
using System.Collections.Generic;
using Godot;

public partial class MainSceneScript : Node3D
{
  public readonly Dictionary<string, Faction> factionMap = new Dictionary<string, Faction>();
  public readonly Dictionary<long, PlayerScript> playerMap = new Dictionary<long, PlayerScript>();
  readonly PackedScene orePrefab = GD.Load<PackedScene>("res://prefabs/ore.tscn");
  readonly ENetMultiplayerPeer peer = new ENetMultiplayerPeer();
  readonly PackedScene playerPrefab = GD.Load<PackedScene>("res://prefabs/player.tscn");

  public static NavigationRegion3D? navigationRegion;

  public override void _EnterTree()
  {
    navigationRegion = GetNode<NavigationRegion3D>("NavigationRegion3D");
  }

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

  Faction Addfaction()
  {
    var uuid = Guid.NewGuid().ToString();

    var faction = new Faction
    {
      id = uuid,
      money = 0,
      name = "faction"
    };

    factionMap.Add(uuid, faction);

    return faction;
  }

  void AddPlayer(long id, string factionId)
  {
    var player = playerPrefab.Instantiate<PlayerScript>();
    player.factionId = factionId;
    player.GlobalTransform = new Transform3D(Basis.Identity, Vector3.Zero);
    player.Name = $"Player-{id}";

    playerMap.Add(id, player);

    CallDeferred(Node.MethodName.AddChild, player);
  }

  void Host()
  {
    peer.CreateServer(5001);
    Multiplayer.MultiplayerPeer = peer;

    Multiplayer.PeerConnected += (long id) =>
    {
      var faction = Addfaction();

      AddPlayer(id, faction.id);
    };

    var faction = Addfaction();

    AddPlayer(1, faction.id);

    SpawnOres();

    navigationRegion?.CallDeferred(NavigationRegion3D.MethodName.BakeNavigationMesh);
  }

  void Join()
  {
    peer.CreateClient("localhost", 5001);
    Multiplayer.MultiplayerPeer = peer;
  }

  void SpawnOres()
  {
    for (int i = 0; i < 10; i++)
    {
      const int radius = 20;

      var position = new Vector3
      {
        X = (Random.Shared.NextSingle() * radius) - radius / 2,
        Y = 10,
        Z = (Random.Shared.NextSingle() * radius) - radius / 2,
      };

      var query = PhysicsRayQueryParameters3D.Create(position, position + Vector3.Down * 100);

      var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
      if (result.Count == 0)
      {
        return;
      }

      var hitPosition = (Vector3)result["position"];

      var ore = orePrefab.Instantiate<Node3D>();
      ore.GlobalTransform = new Transform3D(Basis.Identity, hitPosition);
      ore.Name = "Ore";

      navigationRegion?.CallDeferred(Node.MethodName.AddChild, ore, true);
    }
  }
}
