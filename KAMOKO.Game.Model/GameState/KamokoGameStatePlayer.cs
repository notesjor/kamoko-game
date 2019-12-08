using System;
using KAMOKO.Game.Model.GameState.Abstract;

namespace KAMOKO.Game.Model.GameState
{
  public class KamokoGameStatePlayer : AbstractKamokoGameState
  {
    public Guid PlayerId { get; set; } = Guid.NewGuid();
    public string PlayerName { get; set; } = "PLAYER";
  }
}