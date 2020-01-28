using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KAMOKO.Game.Model.GameFile;
using KAMOKO.Game.Model.GameMode;
using KAMOKO.Game.Model.GameState;
using KAMOKO.Game.Model.Request;
using KAMOKO.Game.Model.Response;
using Newtonsoft.Json;
using Tfres;

namespace KAMOKO.Game.Server
{
  public class KamokoWebService
  {
    private Dictionary<string, QuestSentence[]> _courses = new Dictionary<string, QuestSentence[]>();
    private Dictionary<Guid, KamokoGameStateServer> _gamesPublic = new Dictionary<Guid, KamokoGameStateServer>();
    private Dictionary<Guid, KamokoGameStateServer> _gamesPrivate = new Dictionary<Guid, KamokoGameStateServer>();
    private Tfres.Server _server;
    private BackgroundWorker _worker;
    private int _workerTimespan;
    private object _lock = new object();
    private string _courseList;
    private Random _rnd = new Random();

    public KamokoWebService()
    {
      LoadCourses();
    }

    public void Start()
    {
      var c = JsonConvert.DeserializeObject<KamokoWebServiceConfiguration>(File.ReadAllText("webService.json", Encoding.UTF8));
      _workerTimespan = c.BackgroundWorkerTimespan;

      Console.Write($"Run KAMOKO.Game.Server on {c.Ip}:{c.Port}...");
      _server = new Tfres.Server(c.Ip, c.Port, (arg) => arg.Response.Send(500));

      _server.AddEndpoint(HttpVerb.GET, "/ping", (arg) => arg.Response.Send(200));
      _server.AddEndpoint(HttpVerb.GET, "/time", (arg) => arg.Response.Send(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")));
      _server.AddEndpoint(HttpVerb.GET, "/courses", (arg) => arg.Response.Send(_courseList));
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
                _gamesPrivate.Remove(x);
        }
        catch
        {
          // ignore
        }

        Thread.Sleep(_workerTimespan);
      }
    }

    private Guid[] CleanupWorkFindObsoleteItems(ref Dictionary<Guid, KamokoGameStateServer> dic)
    {
      try
      {
        var res = new List<Guid>();
        foreach (var item in dic)
        {
          if (item.Value.End < DateTime.Now)
            res.Add(item.Key);
        }

        return res.ToArray();
      }
      catch
      {
        return new Guid[0];
      }
    }

    private Task StatisticsGameRequest(HttpContext arg)
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
                   ? _gamesPrivate[Guid.Parse(gameId)] 
                   : _gamesPublic[Guid.Parse(gameId)];

        return mode == "private"
                 ? game.AdminId != Guid.Parse(adminId)
                     ? arg.Response.Send(501)
                     : arg.Response.Send(game.Answers.Select(x => x.Response).ToArray())
                 : arg.Response.Send(game.Answers.Select(x => x.Response).OrderByDescending(x => x.Score).Take(10).ToArray());
      }
      catch
      {
        return arg.Response.Send(500);
      }
    }

    private Task SubmitGameRequest(HttpContext arg)
    {
      try
      {
        var req = arg.PostData<SubmitGameRequest>();
        KamokoGameStateServer game;
        lock (_lock)
          game = _gamesPrivate[req.GameId];

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

        return arg.Response.Send(resp);
      }
      catch
      {
        return arg.Response.Send(500);
      }
    }

    private Task StartGameRequest(HttpContext arg)
    {
      try
      {
        var get = arg.GetData();
        var gameId = get["gameid"];
        var adminId = get["adminid"];

        KamokoGameStateServer game;
        lock (_lock)
          game = _gamesPrivate[Guid.Parse(gameId)];

        if (game.AdminId != Guid.Parse(adminId))
          return arg.Response.Send(501);

        game.Start = DateTime.Now.AddSeconds(15);
        return arg.Response.Send(200);
      }
      catch
      {
        return arg.Response.Send(500);
      }
    }

    private Task WaitGameRequest(HttpContext arg)
    {
      try
      {
        var gameId = arg.GetData()["gameid"];
        bool notStarted;
        DateTime startTime;
        lock (_lock)
        {
          startTime = _gamesPrivate[Guid.Parse(gameId)].Start;
          notStarted = startTime == DateTime.MaxValue;
        }

        return notStarted
                 ? arg.Response.Send(201)
                 : arg.Response.Send((startTime - DateTime.Now).Seconds.ToString());
      }
      catch
      {
        return arg.Response.Send(500);
      }
    }

    private Task CreateGameRequest(HttpContext arg)
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
              _gamesPublic.Add(Guid.NewGuid(), state);

            return arg.Response.Send(state.CreatePlayerState());
          }
          else
          {
            var state = NewGameState(req, DateTime.MaxValue, DateTime.Now.AddDays(8));
            lock (_lock)
            {
              var guid = Guid.NewGuid();
              var num = state.GameId;

              _gamesPrivate.Add(Guid.NewGuid(), state);
            }

            return arg.Response.Send($"#GAME#{state.GameId}-{state.AdminId}");
          }
        }
        else
        {
          var state = NewGameState(req, DateTime.Now.AddSeconds(10), DateTime.Now.AddMinutes(45));
          lock (_lock)
            _gamesPrivate.Add(Guid.NewGuid(), state);

          return arg.Response.Send(state.CreatePlayerState());
        }
      }
      catch
      {
        return arg.Response.Send(500);
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

        var idx = _rnd.Next(0, init.Count);
        var tmp = init[idx];
        init.RemoveAt(idx);
        rand.Add(tmp);
      }

      var select = new QuestSentence[req.Questions];
      for (var i = 0; i < select.Length; i++)
      {
        var idx = _rnd.Next(0, rand.Count);
        var tmp = rand[idx];
        rand.RemoveAt(idx);
        select[i] = course[tmp];
      }
      #endregion

      return new KamokoGameStateServer
      {
        AdminId = Guid.NewGuid(),
        Course = req.Course,
        Difficult = req.Difficult,
        Start = start,
        End = end,
        GameId = Guid.NewGuid(),
        Questions = select,
        Timeout = req.Timeout < 5 ? 5 : req.Timeout
      };
    }

    private Task JoinGameRequest(HttpContext arg)
    {
      try
      {
        var req = arg.PostData<JoinGameRequest>();

        if (req.IsPrivateGame)
        {
          lock (_lock)
            return arg.Response.Send(_gamesPrivate[req.GameId].CreatePlayerState());
        }
        else
        {
          lock (_lock)
            return arg.Response.Send(_gamesPublic[req.GameId].CreatePlayerState());
        }
      }
      catch
      {
        return arg.Response.Send(500);
      }
    }

    private Task ListOpenGamesRequest(HttpContext arg)
    {
      try
      {
        lock (_lock)
          return arg.Response.Send(_gamesPublic.Values.Reverse().Take(25).Select(q => new OpenGameResponse
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
        return arg.Response.Send(500);
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
