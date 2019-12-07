using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KAMOKO.Game.Model.Response
{
  public class OpenGameResponse
  {
    public DateTime Start { get; set; }
    public string Course { get; set; }
    public int Questions { get; set; }
    public int Timeout { get; set; }
  }
}
