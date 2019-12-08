using KAMOKO.Game.Model.GameState.Abstract;
using KAMOKO.Game.Model.Helper;

namespace KAMOKO.Game.Model.GameState
{
  public class KamokoGameStatePlayer : AbstractKamokoGameState
  {
    public int PlayerId { get; set; } = RandomHelper.GetNumber();
    public string PlayerName { get; set; } = "#ID:" + RandomHelper.GetGuid();
  }
}