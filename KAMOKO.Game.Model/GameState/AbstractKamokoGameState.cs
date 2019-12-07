using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using KAMOKO.Game.Model.GameFile;

namespace KAMOKO.Game.Model.GameState
{
  public abstract class AbstractKamokoGameState
  {
    public int GameId { get; set; }
    public string Course { get; set; }
    public DateTime Start { get; set; }
    public QuestSentence[] Questions { get; set; }
    public int Timeout { get; set; }
    public int Difficult { get; set; }
  }
}
