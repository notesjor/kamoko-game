using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KAMOKO.Game.Server
{
  public class KamokoWebServiceConfiguration
  {
    public string Ip { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 3030;
    public int BackgroundWorkerTimespan { get; set; } = 60000;
  }
}
