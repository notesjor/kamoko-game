using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KAMOKO.Game.Model.GameFile;
using KAMOKO.Game.Model.GameState;
using KAMOKO.Game.Model.Request;

namespace KAMOKO.Game.Model.GameMode.Abstract
{
  public abstract class AbstractKamokoGameMode
  {
    public abstract void Calculate(ref KamokoGameStateServer state, ref SubmitGameRequest submit, out int score, out int total);
  }
}
