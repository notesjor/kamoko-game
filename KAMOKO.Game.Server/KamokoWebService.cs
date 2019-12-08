using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using KAMOKO.Game.Model.GameFile;
using KAMOKO.Game.Model.GameMode;
using KAMOKO.Game.Model.GameState;
using KAMOKO.Game.Model.Helper;
using KAMOKO.Game.Model.Request;
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
      _server.AddEndpoint(HttpVerb.GET, "/courses", (arg) => new HttpResponse(arg, true, 200, _courseList));
      _server.AddEndpoint(HttpVerb.GET, "/opengames", ListOpenGamesRequest);
      _server.AddEndpoint(HttpVerb.POST, "/join", JoinGameRequest);
      _server.AddEndpoint(HttpVerb.POST, "/new", CreateGameRequest);
      _server.AddEndpoint(HttpVerb.GET, "/wait", WaitGameRequest);
      _server.AddEndpoint(HttpVerb.GET, "/start", StartGameRequest);
      _server.AddEndpoint(HttpVerb.POST, "/submit", SubmitGameRequest);
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
      try
      {
        var res = new List<string>();
        foreach (var item in dic)
        {
          if (item.Value.End < DateTime.Now)
            res.Add(item.Key);
        }

        return res.ToArray();
      }
      catch
      {
        return new string[0];
      }
    }

    private HttpResponse StatisticsGameRequest(HttpRequest arg)
    {
      try
      {
        var get = arg.GetData();
        var gameId = get["gameid"];
        var adminId = get["adminid"];
        var mode = get["mode"];

        KamokoGameStateServer game;
        lock (_lock)
          game = mode == "private" 
                   ? _gamesPrivate[_gamesPrivateResolver[int.Parse(gameId)]] 
                   : _gamesPublic[gameId];

        if (game.AdminId != int.Parse(adminId))
          return new HttpResponse(arg, false, 501, "wrong AdminId");


      }
      catch
      {
        return new HttpResponse(arg, false, 500);
      }
    }

    private HttpResponse SubmitGameRequest(HttpRequest arg)
    {
      try
      {
        var req = arg.PostData<SubmitGameRequest>();
        KamokoGameStateServer game;
        lock (_lock)
          game = _gamesPrivate[_gamesPrivateResolver[int.Parse(req.GameId)]];

        int total = 0, score = 0;
        switch (game.Difficult)
        {
          case 1:
          case 2:
            new KamokoGameModeEasy().Calculate(ref game, ref req, out score, out total);
            break;
          case 3:
          case 4:
            new KamokoGameModeComplex().Calculate(ref game, ref req, out score, out total);
            break;
        }

        var resp = new SubmitGameResponse { Score = score, Total = total };
        lock (_lock)
          game.Answers.Add(new ExtendedSubmitGameRequest { Request = req, Response = resp });

        return new HttpResponse(arg, true, 200, resp);
      }
      catch
      {
        return new HttpResponse(arg, false, 500);
      }
    }

    private HttpResponse StartGameRequest(HttpRequest arg)
    {
      try
      {
        var get = arg.GetData();
        var gameId = get["gameid"];
        var adminId = get["adminid"];

        KamokoGameStateServer game;
        lock (_lock)
          game = _gamesPrivate[_gamesPrivateResolver[int.Parse(gameId)]];

        if (game.AdminId != int.Parse(adminId))
          return new HttpResponse(arg, false, 501, "wrong AdminId");

        game.Start = DateTime.Now.AddSeconds(15);
        return new HttpResponse(arg, true, 200);
      }
      catch
      {
        return new HttpResponse(arg, false, 500);
      }
    }

    private HttpResponse WaitGameRequest(HttpRequest arg)
    {
      try
      {
        var gameId = arg.GetData()["gameid"];
        bool notStarted;
        DateTime startTime;
        lock (_lock)
        {
          startTime = _gamesPrivate[_gamesPrivateResolver[int.Parse(gameId)]].Start;
          notStarted = startTime == DateTime.MaxValue;
        }

        return notStarted
                 ? new HttpResponse(arg, true, 201, "wait")
                 : new HttpResponse(arg, true, 200, (startTime - DateTime.Now).Seconds.ToString());
      }
      catch
      {
        return new HttpResponse(arg, false, 500);
      }
    }

    private HttpResponse CreateGameRequest(HttpRequest arg)
    {
      try
      {
        var req = arg.PostData<CreateGameRequest>();

        if (req.IsMultiPlayer)
        {
          if (req.IsOpen)
          {
            var state = NewGameState(req, DateTime.Now.AddSeconds(90), DateTime.Now.AddMinutes(45));
            lock (_lock)
              _gamesPublic.Add(RandomHelper.GetGuid(), state);

            return new HttpResponse(arg, true, 200, state.CreatePlayerState());
          }
          else
          {
            var state = NewGameState(req, DateTime.MaxValue, DateTime.Now.AddDays(8));
            lock (_lock)
            {
              var guid = RandomHelper.GetGuid();
              var num = state.GameId;

              _gamesPrivate.Add(RandomHelper.GetGuid(), state);
            }

            return new HttpResponse(arg, true, 200, $"#GAME#{state.GameId}-{state.AdminId}");
          }
        }
        else
        {
          var state = NewGameState(req, DateTime.Now.AddSeconds(10), DateTime.Now.AddMinutes(45));
          lock (_lock)
            _gamesPrivate.Add(RandomHelper.GetGuid(), state);

          return new HttpResponse(arg, true, 200, state.CreatePlayerState());
        }
      }
      catch
      {
        return new HttpResponse(arg, false, 500);
      }
    }

    private KamokoGameStateServer NewGameState(CreateGameRequest req, DateTime start, DateTime end)
    {
      var course = _courses[req.Course];

      #region Random Selection
      var init = new List<int>();
      for (int i = 0; i < course.Length; i++)
        init.Add(i);

      var rand = new List<int>();
      while (init.Count > 0)
      {
        if (init.Count == 1)
        {
          rand.Add(init[0]);
          break;
        }

        var idx = RandomHelper.GetNumber(0, init.Count);
        var tmp = init[idx];
        init.RemoveAt(idx);
        rand.Add(tmp);
      }

      var select = new QuestSentence[req.Questions];
      for (var i = 0; i < select.Length; i++)
      {
        var idx = RandomHelper.GetNumber(0, rand.Count);
        var tmp = rand[idx];
        rand.RemoveAt(idx);
        select[i] = course[tmp];
      }
      #endregion

      return new KamokoGameStateServer
      {
        AdminId = RandomHelper.GetNumber(),
        Course = req.Course,
        Difficult = req.Difficult,
        Start = start,
        End = end,
        GameId = RandomHelper.GetNumber(),
        Questions = select,
        Timeout = req.Timeout < 5 ? 5 : req.Timeout
      };
    }

    private HttpResponse JoinGameRequest(HttpRequest arg)
    {
      try
      {
        var req = arg.PostData<JoinGameRequest>();

        if (req.IsPrivateGame)
        {
          lock (_lock)
            return new HttpResponse(arg, true, 200, _gamesPrivate[_gamesPrivateResolver[int.Parse(req.GameId)]].CreatePlayerState());
        }
        else
        {
          lock (_lock)
            return new HttpResponse(arg, true, 200, _gamesPublic[req.GameId].CreatePlayerState());
        }
      }
      catch
      {
        return new HttpResponse(arg, false, 500);
      }
    }

    private HttpResponse ListOpenGamesRequest(HttpRequest arg)
    {
      try
      {
        lock (_lock)
          return new HttpResponse(arg, true, 200, _gamesPublic.Values.Reverse().Take(25).Select(q => new OpenGameResponse
          {
            Course = q.Course,
            Questions = q.Questions.Length,
            Start = q.Start,
            Timeout = q.Timeout,
            Difficult = q.Difficult
          }).ToArray());
      }
      catch
      {
        return new HttpResponse(arg, false, 500);
      }
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
