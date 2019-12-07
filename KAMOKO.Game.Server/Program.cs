using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KAMOKO.Game.Model;
using KAMOKO.Game.Model.GameFile;
using KAMOKO.Game.Model.GameState;
using Newtonsoft.Json;

namespace KAMOKO.Game.Server
{
  class Program
  {
    static void Main()
    {
      EnsureDirectories();
      StartServer();
    }

    private static void StartServer()
    {
      var service = new KamokoWebService();
      service.Start();
    }

    private static void EnsureDirectories()
    {
      if (!Directory.Exists("courses"))
        Directory.CreateDirectory("courses");
      if (!Directory.Exists("gamesPublic"))
        Directory.CreateDirectory("gamesPublic");
      if (!Directory.Exists("gamesPrivate"))
        Directory.CreateDirectory("gamesPrivate");
    }
  }
}
