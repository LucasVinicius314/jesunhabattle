using Godot;

public partial class PlayerUiScript : Control
{
  public Label? moneyLabel;

  public override void _Ready()
  {
    moneyLabel = FindChild("MoneyLabel") as Label;
  }
}