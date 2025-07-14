using Godot;

static class Extensions
{
  public static bool IsOre(this Node3D value)
  {
    return value.HasMeta("ore") && value.GetMeta("ore").AsBool();
  }

  public static bool IsUnit(this Node3D value)
  {
    return value.HasMeta("unit") && value.GetMeta("unit").AsBool();
  }
}