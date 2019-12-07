using System;
using System.Linq;

namespace KAMOKO.Game.Model.GameState
{
  public class KamokoGameStateServer : AbstractKamokoGameState
  {
    public int AdminId { get; set; }
    public DateTime End { get; set; }

    public KamokoGameStatePlayer CreatePlayerState()
    {
      return new KamokoGameStatePlayer
      {
        GameId = GameId,
        Start = Start,
        Questions = Questions.Select(x => x.GetGameFile()).ToArray(),
        Timeout = Timeout,
        Course = Course
      };
    }
  }
}