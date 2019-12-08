using System;
using System.Collections.Generic;
using System.Linq;
using KAMOKO.Game.Model.GameState.Abstract;
using KAMOKO.Game.Model.Request;

namespace KAMOKO.Game.Model.GameState
{
  public class KamokoGameStateServer : AbstractKamokoGameState
  {
    public Guid AdminId { get; set; }
    public DateTime End { get; set; }

    public List<ExtendedSubmitGameRequest> Answers { get; set; } = new List<ExtendedSubmitGameRequest>();

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