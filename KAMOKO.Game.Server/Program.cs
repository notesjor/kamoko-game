using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KAMOKO.Game.Server
{
  class Program
  {
    static void Main(string[] args)
    {
      LoadCourses();
    }

    private static void LoadCourses()
    {
      Console.WriteLine("COURSES:");

      var files = Directory.GetFiles("courses", "*.kamokoQuest");
      foreach (var file in files)
      {
        Console.Write($"{file}...");
        try
        {


          Console.WriteLine("ok!");
        }
        catch(Exception ex)
        {
          Console.WriteLine("error!");
          Console.WriteLine(ex.Message);
          Console.WriteLine(ex.StackTrace);
        }
      }

      Console.WriteLine($"{} COURSES AVAILABLE!");
    }
  }
}
