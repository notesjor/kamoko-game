using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KAMOKO.Game.Model.Request
{
  public class CreateGameRequest
  {
    public bool IsMultiPlayer { get; set; }
    public bool IsOpen { get; set; }
    public string Course { get;set;}
    public int Questions { get;set;}
    public int Difficult { get; set; }
    public int Timeout { get; set; }
  }
}
