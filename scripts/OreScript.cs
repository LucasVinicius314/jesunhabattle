using Godot;

public partial class OreScript : Node3D
{
  MainSceneScript mainSceneScript;

  long hp = 5;

  public override void _Ready()
  {
    if (!Multiplayer.IsServer())
    {
      return;
    }

    mainSceneScript = GetNode<MainSceneScript>("/root/MainScene");
  }

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
  void ServerHandleTakeDamageRpc()
  {
    hp--;

    if (hp == 0)
    {
      QueueFree();

      var playerScript = mainSceneScript.playerMap[Multiplayer.GetRemoteSenderId()];
      var faction = mainSceneScript.factionMap[playerScript.factionId];

      faction.money += 1;

      playerScript.SendGameStateToClient(Multiplayer.GetRemoteSenderId());
    }
  }

  public void TakeDamage()
  {
    if (!IsInstanceValid(this))
    {
      return;
    }

    RpcId(1, MethodName.ServerHandleTakeDamageRpc);
  }
}
