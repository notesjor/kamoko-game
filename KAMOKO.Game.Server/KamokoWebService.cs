using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using KAMOKO.Game.Model.GameFile;
using KAMOKO.Game.Model.GameState;
using KAMOKO.Game.Model.Response;
using Newtonsoft.Json;
using Tfres;

namespace KAMOKO.Game.Server
{
  public class KamokoWebService
  {
    private Dictionary<string, QuestSentence[]> _courses = new Dictionary<string, QuestSentence[]>();
    private Dictionary<string, KamokoGameStateServer> _gamesPublic = new Dictionary<string, KamokoGameStateServer>();
    private Dictionary<string, KamokoGameStateServer> _gamesPrivate = new Dictionary<string, KamokoGameStateServer>();
    private Dictionary<int, string> _gamesPrivateResolver = new Dictionary<int, string>();
    private Tfres.Server _server;
    private BackgroundWorker _worker;
    private int _workerTimespan;
    private object _lock = new object();
    private string _courseList;

    public KamokoWebService()
    {
      LoadCourses();
      LoadGames();
    }

    public void Start()
    {
      var c = JsonConvert.DeserializeObject<KamokoWebServiceConfiguration>(File.ReadAllText("webService.json", Encoding.UTF8));
      _workerTimespan = c.BackgroundWorkerTimespan;

      Console.Write($"Run KAMOKO.Game.Server on {c.Ip}:{c.Port}...");
      _server = new Tfres.Server(c.Ip, c.Port, (arg) => new HttpResponse(arg, false, 500, null, null, null));

      _server.AddEndpoint(HttpVerb.GET, "/ping", (arg) => new HttpResponse(arg, true, 200));
      _server.AddEndpoint(HttpVerb.GET, "/time", (arg) => new HttpResponse(arg, true, 200, DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")));
      _server.AddEndpoint(HttpVerb.GET, "/list", ListOpenGamesRequest);
      _server.AddEndpoint(HttpVerb.GET, "/join", JoinGameRequest);
      _server.AddEndpoint(HttpVerb.GET, "/courses", (arg) => new HttpResponse(arg, true, 200, _courseList));
      _server.AddEndpoint(HttpVerb.GET, "/new", CreateGameRequest);
      _server.AddEndpoint(HttpVerb.GET, "/start", StartGameRequest);
      _server.AddEndpoint(HttpVerb.GET, "/submit", SubmitGameRequest);
      _server.AddEndpoint(HttpVerb.GET, "/stats", StatisticsGameRequest);

      Console.WriteLine("ok!");

      _worker = new BackgroundWorker();
      _worker.DoWork += CleanupWorkerCall;
      _worker.RunWorkerAsync();

      while (true)
      {
        var command = Console.ReadLine();
        if (command == "quit" || command == "exit")
          break;
      }

      _server.Dispose();
    }

    private void CleanupWorkerCall(object sender, DoWorkEventArgs e)
    {
      while (true)
      {
        try
        {
          // ReSharper disable once InconsistentlySynchronizedField
          var del = CleanupWorkFindObsoleteItems(ref _gamesPublic);
          if (del != null && del.Length > 0)
            lock (_lock)
              foreach (var x in del)
                _gamesPrivate.Remove(x);

          // ReSharper disable once InconsistentlySynchronizedField
          del = CleanupWorkFindObsoleteItems(ref _gamesPrivate);
          if (del != null && del.Length > 0)
            lock (_lock)
              foreach (var x in del)
              {
                _gamesPrivate.Remove(x);
                _gamesPrivateResolver.Remove(_gamesPrivate[x].GameId);
              }
        }
        catch
        {
          // ignore
        }

        Thread.Sleep(_workerTimespan);
      }
    }

    private string[] CleanupWorkFindObsoleteItems(ref Dictionary<string, KamokoGameStateServer> dic)
    {
      var res = new List<string>();
      foreach (var item in dic)
      {
        if (item.Value.End < DateTime.Now)
          res.Add(item.Key);
      }
      return res.ToArray();
    }

    private HttpResponse StatisticsGameRequest(HttpRequest arg)
    {

    }

    private HttpResponse SubmitGameRequest(HttpRequest arg)
    {

    }

    private HttpResponse StartGameRequest(HttpRequest arg)
    {

    }

    private HttpResponse CreateGameRequest(HttpRequest arg)
    {

    }

    private HttpResponse JoinGameRequest(HttpRequest arg)
    {

    }

    private HttpResponse ListOpenGamesRequest(HttpRequest arg)
    {
      var data = _gamesPublic.Values.Take(25).Select(q => new OpenGameResponse
      {
        Course = q.Course,
        Questions = q.Questions.Length
      }).ToArray();
      return new HttpResponse(arg, true, 200, data);
    }

    private void LoadGames()
    {
      Console.WriteLine("RESTORED GAMES:");

      LoadGames("gamesPublic", ref _gamesPublic);
      LoadGames("gamesPrivate", ref _gamesPrivate);

      Console.WriteLine($"{(_gamesPublic.Count + _gamesPrivate.Count):D3} GAMES RESTORED!");

      _courseList = JsonConvert.SerializeObject(_courses.Keys.ToArray());
    }

    private void LoadGames(string dir, ref Dictionary<string, KamokoGameStateServer> gamesPublic)
    {
      var files = Directory.GetFiles(dir, "*.game");
      foreach (var file in files)
      {
        Console.Write($"{file}...");

        try
        {
          var game = JsonConvert.DeserializeObject<KamokoGameStateServer>(File.ReadAllText(file, Encoding.UTF8));
          if (game.End < DateTime.Now)
          {
            File.Delete(file);
            Console.WriteLine($"ok! > game deleted ({game.End})");
            continue;
          }
          gamesPublic.Add(Path.GetFileNameWithoutExtension(file), game);
          Console.WriteLine("ok!");
        }
        catch
        {
          Console.WriteLine("error!");
        }
      }
    }

    private void LoadCourses()
    {
      Console.WriteLine("AVAILABLE COURSES:");

      var files = Directory.GetFiles("courses", "*.kamokoQuest");
      foreach (var file in files)
      {
        Console.Write($"{file}...");
        try
        {
          _courses.Add(Path.GetFileNameWithoutExtension(file),
                       JsonConvert.DeserializeObject<QuestSentence[]>(File.ReadAllText(file, Encoding.UTF8)));

          Console.WriteLine("ok!");
        }
        catch (Exception ex)
        {
          Console.WriteLine("error!");
          Console.WriteLine(ex.Message);
          Console.WriteLine(ex.StackTrace);
        }
      }

      Console.WriteLine($"{_courses.Count:D2} COURSES AVAILABLE!");
    }
  }
}
